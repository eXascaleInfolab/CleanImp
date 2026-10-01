#!/usr/bin/env python3
import argparse,csv,json,math,re,sys,zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

NS={"m":"http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
CONFIGS=[
 ("cla_up.xlsx","Upstream","Classification","upstream"),
 ("cla_down.xlsx","Downstream","Classification","downstream"),
 ("for_up.xlsx","Upstream","Forecasting","upstream"),
 ("for_down.xlsx","Downstream","Forecasting","downstream"),
]

def colnum(ref):
 s=re.match(r"[A-Z]+",ref).group(); n=0
 for c in s:n=n*26+ord(c)-64
 return n

def norm(v):
 if v is None:return None
 if isinstance(v,str) and v.strip().lower() in {"","nan","none","null","-","–","—","#name?","#n/a","#value!","#ref!","#div/0!"}:return None
 try:
  x=float(v); return x if math.isfinite(x) else None
 except (TypeError,ValueError):return None

def first_sheet(path):
 with zipfile.ZipFile(path) as z:
  ss=[]
  if "xl/sharedStrings.xml" in z.namelist():
   root=ET.fromstring(z.read("xl/sharedStrings.xml"))
   ss=["".join(t.text or "" for t in x.findall(".//m:t",NS)) for x in root.findall("m:si",NS)]
  wb=ET.fromstring(z.read("xl/workbook.xml"))
  rel=ET.fromstring(z.read("xl/_rels/workbook.xml.rels"))
  rns={"r":"http://schemas.openxmlformats.org/package/2006/relationships"}
  rmap={x.attrib["Id"]:x.attrib["Target"] for x in rel.findall("r:Relationship",rns)}
  sh=wb.find(".//m:sheets/m:sheet",NS)
  rid=sh.attrib["{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id"]
  target=rmap[rid].lstrip("/")
  if not target.startswith("xl/"):target="xl/"+target
  root=ET.fromstring(z.read(target)); rows=[]
  for row in root.findall(".//m:sheetData/m:row",NS):
   d={}
   for c in row.findall("m:c",NS):
    i=colnum(c.attrib["r"]); typ=c.attrib.get("t"); raw=c.findtext("m:v",default="",namespaces=NS)
    if typ=="s" and raw!="":v=ss[int(raw)]
    elif typ=="inlineStr":v="".join(t.text or "" for t in c.findall(".//m:t",NS))
    elif raw=="":v=None
    else:
     try:v=float(raw)
     except ValueError:v=raw
    d[i]=v
   rows.append(d)
  return rows

def upstream(path):
 rows=first_sheet(path); h=rows[0]
 cols={str(h[c]).strip():c for c in sorted(h) if c>=5 and h[c] is not None}
 out={}
 for r in rows[1:]:
  ds,pat,rate=r.get(1),r.get(2),norm(r.get(3))
  if ds is None or pat is None or rate is None:continue
  for alg,c in cols.items():
   k=(str(ds).strip(),str(pat).strip(),alg,float(rate))
   if k in out:raise ValueError(f"{path.name}: duplicate key {k}")
   out[k]=norm(r.get(c))
 return out

def downstream(path):
 rows=first_sheet(path); h=rows[0]
 def find(names):
  names={x.lower() for x in names}
  return next((c for c,v in h.items() if v is not None and str(v).strip().lower() in names),None)
 dc=find({"dataset","datasets"}); pc=find({"pattern","patterns"}); rc=find({"rate","rates","miss_rate","missing_rate"})
 ac=find({"imputer","imputers","algorithm","algorithms","imp_alg","imp_algs"})
 if None in (dc,pc,rc,ac):raise ValueError(f"{path.name}: cannot identify dataset/pattern/rate/imputer columns")
 meta={dc,pc,rc,ac}
 models={str(h[c]).strip():c for c in sorted(h) if c not in meta and h[c] is not None and str(h[c]).strip()}
 out={}
 for r in rows[1:]:
  ds,pat,rate,alg=r.get(dc),r.get(pc),norm(r.get(rc)),r.get(ac)
  if ds is None or pat is None or rate is None or alg is None:continue
  for model,c in models.items():
   k=(str(ds).strip(),str(pat).strip(),str(alg).strip(),model,float(rate))
   if k in out:raise ValueError(f"{path.name}: duplicate key {k}")
   out[k]=norm(r.get(c))
 return out

def dashboard(path,experiment,task):
 db=json.loads(path.read_text(encoding="utf-8")); out={}
 for r in db["results"]:
  if r.get("experiment")!=experiment or r.get("task")!=task:continue
  base=(str(r["dataset"]).strip(),str(r["pattern"]).strip(),str(r["algo"]).strip())
  if experiment=="Downstream":
   model=r.get("classifier") if task=="Classification" else r.get("forecaster")
   if model is None:raise ValueError(f"data.json: missing downstream model: {r}")
   k=base+(str(model).strip(),float(r["rate"]))
  else:k=base+(float(r["rate"]),)
  if k in out:raise ValueError(f"data.json: duplicate key {k}")
  out[k]=norm(r.get("value"))
 return out

def compare(a,b,tol):
 errors=[]; matches=0
 for k in set(a)|set(b):
  if k not in a:errors.append((k,None,b[k],"EXTRA_IN_JSON"))
  elif k not in b:errors.append((k,a[k],None,"MISSING_IN_JSON"))
  elif (a[k] is None)!=(b[k] is None):errors.append((k,a[k],b[k],"MISSINGNESS_MISMATCH"))
  elif a[k] is None or math.isclose(a[k],b[k],rel_tol=0,abs_tol=tol):matches+=1
  else:errors.append((k,a[k],b[k],"VALUE_MISMATCH"))
 return matches,errors

def main():
 ap=argparse.ArgumentParser(description="Verify all CleanImp dashboard values against the four Excel files.")
 ap.add_argument("--dir",default=".")
 ap.add_argument("--tolerance",type=float,default=1e-12)
 args=ap.parse_args(); base=Path(args.dir).resolve(); jp=base/"data.json"
 needed=[jp]+[base/x[0] for x in CONFIGS]; missing=[x.name for x in needed if not x.is_file()]
 if missing:
  print("Missing required file(s): "+", ".join(missing),file=sys.stderr); return 2
 print("="*68+"\n CleanImp dashboard verification\n"+"="*68)
 totals=[0,0,0,0]
 for fn,exp,task,layout in CONFIGS:
  a=upstream(base/fn) if layout=="upstream" else downstream(base/fn)
  b=dashboard(jp,exp,task); matches,errors=compare(a,b,args.tolerance)
  report=base/f"verification_{exp.lower()}_{task.lower()}.csv"
  with report.open("w",newline="",encoding="utf-8") as f:
   w=csv.writer(f); w.writerow(["key","excel","dashboard","status"])
   for k,x,y,s in errors:w.writerow([" | ".join(map(str,k)),x,y,s])
  print(f"\n{exp} / {task}")
  print(f"  Excel entries:             {len(a):>10,}")
  print(f"  Dashboard entries:         {len(b):>10,}")
  print(f"  Matching entries:          {matches:>10,}")
  print(f"  Errors:                    {len(errors):>10,}")
  print(f"  Excel numeric values:      {sum(v is not None for v in a.values()):>10,}")
  print(f"  Dashboard numeric values:  {sum(v is not None for v in b.values()):>10,}")
  print("  "+("✓ PASSED" if not errors else "✗ FAILED"))
  totals[0]+=len(a);totals[1]+=len(b);totals[2]+=matches;totals[3]+=len(errors)
 print("\n"+"="*68+"\n TOTAL")
 print(f"  Excel entries:             {totals[0]:>10,}")
 print(f"  Dashboard entries:         {totals[1]:>10,}")
 print(f"  Matching entries:          {totals[2]:>10,}")
 print(f"  Errors:                    {totals[3]:>10,}")
 print("="*68)
 if totals[3]==0 and totals[0]==totals[1]:
  print(" ✓ ALL VERIFICATIONS PASSED");return 0
 print(" ✗ VERIFICATION FAILED\n Check verification_*.csv for details.");return 1

if __name__=="__main__":raise SystemExit(main())
