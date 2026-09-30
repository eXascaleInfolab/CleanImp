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
function fmt(v){return v===null||v===undefined?"—":Number(v).toFixed(4)}
function renderTable(rows){
 const table=$("values-table");
 table.innerHTML=`<thead><tr><th>Algorithm</th>${DB.rates.map(r=>`<th>${r}</th>`).join("")}</tr></thead>`+
 `<tbody>${rows.map(r=>`<tr><td>${r.algo}</td>${r.values.map(v=>`<td class="${v==null?"missing":""}">${fmt(v)}</td>`).join("")}</tr>`).join("")}</tbody>`;
 $("tablemeta").textContent=`${rows.length} selected algorithm${rows.length===1?"":"s"} · ${$("metric").value}`;
}
function render(){
 const chosen=new Set([...$("algos").querySelectorAll("input:checked")].map(x=>x.value));
 const rows=matching().filter(r=>chosen.has(r.algo));
 const metric=$("metric").value;
 const traces=rows.map(r=>({x:DB.rates,y:r.values,name:r.algo,type:"scatter",mode:"lines+markers",connectgaps:false,
   hovertemplate:`${r.algo}<br>Rate: %{x}<br>${metric}: %{y:.4f}<extra></extra>`}));
 Plotly.react("plot",traces,{margin:{l:72,r:28,t:30,b:78},xaxis:{title:{text:"Missing rate",font:{size:16}},tickvals:DB.rates,tickfont:{size:13}},
   yaxis:{title:{text:metric,font:{size:16}},tickfont:{size:13}},legend:{orientation:"h",y:-.20,font:{size:14},itemsizing:"constant"},hovermode:"closest",paper_bgcolor:"#fff",plot_bgcolor:"#fff"},
   {responsive:true,displaylogo:false});
 $("title").textContent=`${$("dataset").value} · ${$("pattern").value} · ${$("family").value}`;
 $("meta").textContent=`${rows.length} selected algorithm${rows.length===1?"":"s"} · ${$("experiment").value} / ${$("task").value} · ${metric}`;
 renderTable(rows);
}
async function start(){
 DB=await fetch("data.json").then(r=>r.json());
 $("metric").innerHTML='<option value="RMSE">RMSE</option>';
 $("metric").onchange=render;
 F.forEach(f=>{$(f).onchange=cascade});
 F.forEach(setOptions);buildAlgos();render();
 $("all").onclick=()=>{$("algos").querySelectorAll("input").forEach(x=>x.checked=true);render()};
 $("none").onclick=()=>{$("algos").querySelectorAll("input").forEach(x=>x.checked=false);render()};
}
start();