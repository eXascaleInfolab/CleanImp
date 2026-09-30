const $=id=>document.getElementById(id);
let DB;

const uniq=a=>[...new Set(a.filter(v=>v!==null&&v!==undefined&&v!==""))]
  .sort((a,b)=>String(a).localeCompare(String(b)));

function isDownstream(){
  return $("experiment").value==="Downstream";
}
function classifierActive(){
  return isDownstream() && $("task").value==="Classification";
}

function baseMatch(ignore=null){
  const keys=["experiment","task","metric","pattern","dataset"];
  return DB.results.filter(r=>{
    if(!keys.every(f=>f===ignore || !$(f).value || r[f]===$(f).value)) return false;
    if(classifierActive() && ignore!=="classifier" && $("classifier").value && r.classifier!==$("classifier").value) return false;
    if(isDownstream() && ignore!=="model" && $("model").value && r.algo!==$("model").value) return false;
    return true;
  });
}

function setSimpleOptions(id, values, preserve=true){
  const el=$(id), old=el.value;
  el.innerHTML=values.map(v=>`<option value="${v}">${v}</option>`).join("");
  el.value=(preserve && values.includes(old)) ? old : (values[0]||"");
}

function rebuildControls(changed){
  // Experiment must ALWAYS be populated from the full database, otherwise
  // the current selection can accidentally filter Upstream out.
  setSimpleOptions("experiment", uniq(DB.results.map(r=>r.experiment)), true);

  // Task depends only on experiment.
  let rows=DB.results.filter(r=>r.experiment===$("experiment").value);
  setSimpleOptions("task", uniq(rows.map(r=>r.task)), true);

  // Metric depends on experiment + task.
  rows=rows.filter(r=>r.task===$("task").value);
  setSimpleOptions("metric", uniq(rows.map(r=>r.metric)), true);

  const downstream=isDownstream();
  $("classifier-wrap").classList.toggle("hidden",!classifierActive());

  // Classifier only for Downstream + Classification.
  if(classifierActive()){
    const vals=uniq(rows.map(r=>r.classifier));
    setSimpleOptions("classifier",vals,true);
  } else {
    $("classifier").innerHTML="";
  }

  // Pattern: constrain by experiment/task/metric and classifier when relevant.
  let filtered=rows.filter(r=>r.metric===$("metric").value);
  if(classifierActive() && $("classifier").value)
    filtered=filtered.filter(r=>r.classifier===$("classifier").value);
  setSimpleOptions("pattern",uniq(filtered.map(r=>r.pattern)),true);

  filtered=filtered.filter(r=>r.pattern===$("pattern").value);
  setSimpleOptions("dataset",uniq(filtered.map(r=>r.dataset)),true);

  filtered=filtered.filter(r=>r.dataset===$("dataset").value);

  buildAlgos(filtered);
  render();
}

function rowsForView(){
  let rows=DB.results.filter(r=>
    r.experiment===$("experiment").value &&
    r.task===$("task").value &&
    r.metric===$("metric").value &&
    r.pattern===$("pattern").value &&
    r.dataset===$("dataset").value
  );
  if(classifierActive()) rows=rows.filter(r=>r.classifier===$("classifier").value);
  return rows;
}

function buildAlgos(prefiltered=null){
  const old=new Set([...$("algos").querySelectorAll("input:checked")].map(x=>x.value));
  // Show ALL available imputers for this downstream configuration, not just the model dropdown selection.
  const rows=prefiltered || rowsForView();
  const vals=uniq(rows.map(r=>r.algo));
  $("algos").innerHTML=vals.map(v=>
    `<label><input type="checkbox" value="${v}" ${!old.size||old.has(v)?"checked":""}> ${v}</label>`
  ).join("");
  $("algos").querySelectorAll("input").forEach(x=>x.onchange=render);
}

function groupedSeries(rows){
  const chosen=new Set([...$("algos").querySelectorAll("input:checked")].map(x=>x.value));
  const map=new Map();
  rows.filter(r=>chosen.has(r.algo)).forEach(r=>{
    if(!map.has(r.algo)) map.set(r.algo,[]);
    map.get(r.algo).push(r);
  });
  return [...map.entries()].map(([algo,rs])=>({algo,rs:rs.sort((a,b)=>a.rate-b.rate)}));
}
function fmt(v){return v===null||v===undefined?"—":Number(v).toFixed(4)}

function renderTable(series){
  const rates=uniq(series.flatMap(s=>s.rs.map(r=>r.rate))).sort((a,b)=>a-b);
  const getv=(s,rate)=>s.rs.find(r=>r.rate===rate)?.value;
  $("values-table").innerHTML=
    `<thead><tr><th>Algorithm</th>${rates.map(r=>`<th>${r}</th>`).join("")}</tr></thead>`+
    `<tbody>${series.map(s=>`<tr><td>${s.algo}</td>${rates.map(rate=>{
      const v=getv(s,rate); return `<td class="${v==null?"missing":""}">${fmt(v)}</td>`;
    }).join("")}</tr>`).join("")}</tbody>`;
  $("tablemeta").textContent=`${series.length} selected algorithm${series.length===1?"":"s"} · ${$("metric").value}`;
}

function render(){
  const series=groupedSeries(rowsForView()), metric=$("metric").value;
  const traces=series.map(s=>({
    x:s.rs.map(r=>r.rate),y:s.rs.map(r=>r.value),name:s.algo,type:"scatter",mode:"lines+markers",connectgaps:false,
    hovertemplate:`${s.algo}<br>Rate: %{x}<br>${metric}: %{y:.4f}<extra></extra>`
  }));
  Plotly.react("plot",traces,{
    margin:{l:72,r:28,t:30,b:78},
    xaxis:{title:{text:"Missing rate",font:{size:16}},tickfont:{size:13}},
    yaxis:{title:{text:metric,font:{size:16}},tickfont:{size:13}},
    legend:{orientation:"h",y:-.20,font:{size:14},itemsizing:"constant"},
    hovermode:"closest",paper_bgcolor:"#fff",plot_bgcolor:"#fff"
  },{responsive:true,displaylogo:false});

  $("title").textContent=`${$("dataset").value} · ${$("pattern").value}`;
  const clf=classifierActive()?` · ${$("classifier").value}`:"";
  $("meta").textContent=`${series.length} selected algorithm${series.length===1?"":"s"} · ${$("experiment").value} / ${$("task").value}${clf} · ${metric}`;
  renderTable(series);
}

async function start(){
  DB=await fetch("data.json").then(r=>r.json());

  ["experiment","task","metric","classifier","pattern","dataset"].forEach(id=>{
    $(id).onchange=()=>rebuildControls(id);
  });

  // Initialize explicitly from the whole database.
  setSimpleOptions("experiment",uniq(DB.results.map(r=>r.experiment)),false);
  rebuildControls("init");

  $("all").onclick=()=>{$("algos").querySelectorAll("input").forEach(x=>x.checked=true);render()};
  $("none").onclick=()=>{$("algos").querySelectorAll("input").forEach(x=>x.checked=false);render()};
}
start();