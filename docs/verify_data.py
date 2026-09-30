#!/usr/bin/env python3
"""
Verify CleanImp benchmark_dashboard/data.json against the source Excel workbook.

Usage:
    python verify_data.py /path/to/imputers3.xlsx
    python verify_data.py /path/to/imputers3.xlsx /path/to/data.json

Place this script in:
    CleanImp/additional_material/benchmark_dashboard/verify_data.py

If data.json is omitted, the script reads data.json next to this script.
SPIRIT is intentionally excluded to match the dashboard.
"""

from pathlib import Path
import sys, json, zipfile, xml.etree.ElementTree as ET, re, math, csv

NS = {"m": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
TOL = 1e-12

FAMILIES = {
    "MeanImpute": "MC", "SoftImpute": "MC", "IterativeSVD": "MC", "GROUSE": "MC",
    "SVT": "MC", "ROSL": "MC", "TRMF": "MC", "CDRec": "MC",
    "STMVL": "PS", "Dynammo": "PS",
    "XGBOOST": "ML", "MissForest": "ML", "MICE": "ML", "IIM": "ML",
    "MRNN": "DL", "BRITS": "DL", "SAITS": "DL", "BitGraph": "DL", "BayOTIDE": "DL",
    "GRIN": "DL", "MPIN": "DL", "MissNet": "DL", "GAIN": "DL", "PriSTI": "DL",
    "CSDI": "DL", "HKMFT": "DL", "DeepMVI": "DL", "TimesNet": "DL",
    "Moment": "LLMs", "NuwaTS": "LLMs", "GPT4TS": "LLMs",
}

def col_num(ref):
    letters = re.match(r"[A-Z]+", ref).group()
    n = 0
    for ch in letters:
        n = n * 26 + ord(ch) - 64
    return n

def read_excel_cached_values(path):
    """Read cached cell values from the XLSX, with no external Python packages."""
    with zipfile.ZipFile(path) as z:
        shared = []
        if "xl/sharedStrings.xml" in z.namelist():
            root = ET.fromstring(z.read("xl/sharedStrings.xml"))
            for si in root.findall("m:si", NS):
                shared.append("".join(t.text or "" for t in si.findall(".//m:t", NS)))

        sheet = ET.fromstring(z.read("xl/worksheets/sheet1.xml"))
        rows = []
        for row in sheet.findall(".//m:sheetData/m:row", NS):
            vals = {}
            for c in row.findall("m:c", NS):
                idx = col_num(c.attrib["r"])
                typ = c.attrib.get("t")
                v = c.findtext("m:v", default="", namespaces=NS)
                if typ == "s" and v != "":
                    val = shared[int(v)]
                elif typ == "inlineStr":
                    val = "".join(t.text or "" for t in c.findall(".//m:t", NS))
                elif v == "":
                    val = None
                else:
                    try:
                        val = float(v)
                        if val.is_integer():
                            val = int(val)
                    except Exception:
                        val = v
                vals[idx] = val
            rows.append(vals)
        return rows

def normalize_number(v):
    if v is None:
        return None
    if isinstance(v, str):
        if v.strip().lower() in {"", "nan", "-", "–", "none", "null"}:
            return None
    try:
        x = float(v)
        return x if math.isfinite(x) else None
    except Exception:
        return None

def excel_map(xlsx):
    rows = read_excel_cached_values(xlsx)
    headers = rows[0]
    algo_cols = {c: name for c, name in headers.items()
                 if c >= 5 and name in FAMILIES and name != "SPIRIT"}

    out = {}
    for r in rows[1:]:
        dataset, pattern, rate = r.get(2), r.get(3), r.get(4)
        if not dataset or not pattern or not isinstance(rate, (int, float)):
            continue
        rate = float(rate)
        for c, algo in algo_cols.items():
            key = ("Upstream", "Classification", str(pattern), str(dataset),
                   FAMILIES[algo], algo, rate)
            out[key] = normalize_number(r.get(c))
    return out

def json_map(json_path):
    db = json.loads(Path(json_path).read_text())
    rates = [float(x) for x in db["rates"]]
    out = {}
    for r in db["results"]:
        if r["algo"] == "SPIRIT":
            continue
        for rate, value in zip(rates, r["values"]):
            key = (r["experiment"], r["task"], r["pattern"], r["dataset"],
                   r["family"], r["algo"], rate)
            out[key] = normalize_number(value)
    return out

def same(a, b):
    if a is None or b is None:
        return a is None and b is None
    return math.isclose(a, b, rel_tol=0.0, abs_tol=TOL)

def main():
    if len(sys.argv) not in (2, 3):
        print("Usage: python verify_data.py /path/to/imputers3.xlsx [data.json]")
        return 2

    xlsx = Path(sys.argv[1]).resolve()
    json_path = Path(sys.argv[2]).resolve() if len(sys.argv) == 3 else Path(__file__).with_name("data.json")

    if not xlsx.exists():
        print(f"ERROR: Excel file not found: {xlsx}")
        return 2
    if not json_path.exists():
        print(f"ERROR: JSON file not found: {json_path}")
        return 2

    excel = excel_map(xlsx)
    dashboard = json_map(json_path)

    all_keys = sorted(set(excel) | set(dashboard), key=str)
    errors = []
    matches = 0

    for key in all_keys:
        in_x = key in excel
        in_j = key in dashboard
        xv = excel.get(key)
        jv = dashboard.get(key)

        if not in_x:
            status = "EXTRA_IN_JSON"
        elif not in_j:
            status = "MISSING_IN_JSON"
        elif not same(xv, jv):
            status = "VALUE_MISMATCH"
        else:
            status = "MATCH"
            matches += 1

        if status != "MATCH":
            errors.append((*key, xv, jv, status))

    err_path = json_path.with_name("verification_errors.csv")
    with err_path.open("w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["experiment","task","pattern","dataset","family","algorithm","rate",
                    "excel_value","json_value","status"])
        w.writerows(errors)

    excel_nonmissing = sum(v is not None for v in excel.values())
    json_nonmissing = sum(v is not None for v in dashboard.values())

    print("=" * 62)
    print(" CleanImp dashboard verification")
    print("=" * 62)
    print(f"Excel entries checked:       {len(excel):>8}")
    print(f"JSON entries checked:        {len(dashboard):>8}")
    print(f"Excel non-missing values:    {excel_nonmissing:>8}")
    print(f"JSON non-missing values:     {json_nonmissing:>8}")
    print(f"Exact/tolerance matches:     {matches:>8}")
    print(f"Mismatches/missing/extra:    {len(errors):>8}")
    print(f"Tolerance:                   {TOL:g}")
    print("-" * 62)

    if errors:
        print("✗ VERIFICATION FAILED")
        print(f"Details: {err_path}")
        for row in errors[:10]:
            print(" ", row)
        if len(errors) > 10:
            print(f"  ... and {len(errors)-10} more")
        return 1

    print("✓ VERIFICATION PASSED")
    print("Every Excel cell represented by the dashboard matches data.json.")
    print(f"Report: {err_path} (header only, no errors)")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
