const $=id=>document.getElementById(id), F=["experiment","task","pattern","dataset","family"];let DB;
const uniq=a=>[...new Set(a)].sort((a,b)=>String(a).localeCompare(String(b)));
function matching(ignore=null){return DB.results.filter(r=>F.every(f=>f===ignore||!$(f).value||r[f]===$(f).value))}
function setOptions(f){const el=$(f),old=el.value,vals=uniq(matching(f).map(r=>r[f]));el.innerHTML=vals.map(v=>`<option value="${v}">${v}</option>`).join("");el.value=vals.includes(old)?old:(vals[0]||"")}
function cascade(){F.forEach(setOptions);buildAlgos();render()}
function buildAlgos(){
 const old=new Set([...$("algos").querySelectorAll("input:checked")].map(x=>x.value));
 const vals=uniq(matching().map(r=>r.algo));
 $("algos").innerHTML=vals.map(v=>`<label><input type="checkbox" value="${v}" ${!old.size||old.has(v)?"checked":""}> ${v}</label>`).join("");
 $("algos").querySelectorAll("input").forEach(x=>x.onchange=render);
}
function render(){
 const chosen=new Set([...$("algos").querySelectorAll("input:checked")].map(x=>x.value));
 const rows=matching().filter(r=>chosen.has(r.algo));
 const traces=rows.map(r=>({x:DB.rates,y:r.values,name:r.algo,type:"scatter",mode:"lines+markers",connectgaps:false,
   hovertemplate:`${r.algo}<br>Rate: %{x}<br>RMSE: %{y:.4f}<extra></extra>`}));
 Plotly.react("plot",traces,{margin:{l:72,r:28,t:30,b:78},xaxis:{title:"Missing rate",tickvals:DB.rates},
   yaxis:{title:"RMSE"},legend:{orientation:"h",y:-.18},hovermode:"closest",paper_bgcolor:"#fff",plot_bgcolor:"#fff"},
   {responsive:true,displaylogo:false});
 $("title").textContent=`${$("dataset").value} · ${$("pattern").value} · ${$("family").value}`;
 $("meta").textContent=`${rows.length} selected algorithm${rows.length===1?"":"s"} · Upstream / Classification`;
}
async function start(){
 DB=await fetch("data.json").then(r=>r.json());
 F.forEach(f=>{$(f).onchange=cascade});
 F.forEach(setOptions);buildAlgos();render();
 $("all").onclick=()=>{$("algos").querySelectorAll("input").forEach(x=>x.checked=true);render()};
 $("none").onclick=()=>{$("algos").querySelectorAll("input").forEach(x=>x.checked=false);render()};
}
start();