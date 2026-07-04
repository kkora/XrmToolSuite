using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace XrmToolSuite.SolutionKnowledgeGraph.Graph
{
    /// <summary>
    /// Renders the graph as a self-contained, offline HTML page: the node/edge data is embedded as JSON
    /// and drawn by a compact vanilla-JS force-directed canvas renderer with search, type filters, node
    /// drag, and click-to-highlight (dependency trace + deletion impact). No external CSS/JS/fonts, so it
    /// opens in any browser. BCL-only (hand-built JSON), so it is unit-testable.
    /// </summary>
    public static class HtmlGraphBuilder
    {
        public static void Export(GraphModel g, string title, string path) =>
            File.WriteAllText(path, Build(g, title), Encoding.UTF8);

        public static string Build(GraphModel g, string title)
        {
            var sb = new StringBuilder();
            sb.Append("<title>").Append(J(title)).Append(" — Knowledge Graph</title>\n");
            sb.Append("<style>").Append(Css).Append("</style>\n");
            sb.Append("<div id=\"bar\">");
            sb.Append("<b>").Append(Html(title)).Append("</b> Knowledge Graph — ");
            sb.Append(g.NodeCount).Append(" nodes, ").Append(g.EdgeCount).Append(" edges. ");
            sb.Append("<input id=\"search\" placeholder=\"Search nodes…\"/> <span id=\"filters\"></span>");
            sb.Append("<span class=\"legend\">Click a node: <b style=\"color:#12a150\">green</b> = it depends on, <b style=\"color:#d13438\">red</b> = impacted by deleting it.</span>");
            sb.Append("</div>\n<canvas id=\"c\"></canvas>\n");
            sb.Append("<script>const DATA=").Append(BuildJson(g)).Append(";\n").Append(Script).Append("</script>\n");
            return sb.ToString();
        }

        private static string BuildJson(GraphModel g)
        {
            var sb = new StringBuilder();
            sb.Append("{\"nodes\":[");
            bool first = true;
            foreach (var n in g.Nodes)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"id\":\"").Append(J(n.Id)).Append("\",\"type\":\"").Append(J(n.Type))
                  .Append("\",\"label\":\"").Append(J(n.Label)).Append("\"}");
            }
            sb.Append("],\"edges\":[");
            first = true;
            foreach (var e in g.Edges)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"s\":\"").Append(J(e.From)).Append("\",\"t\":\"").Append(J(e.To)).Append("\"}");
            }
            sb.Append("]}");
            return sb.ToString();
        }

        // Minimal JSON string escaping.
        private static string J(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length + 8);
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 0x20) sb.Append("\\u").Append(((int)ch).ToString("x4"));
                        else sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }

        private static string Html(string s) => System.Net.WebUtility.HtmlEncode(s ?? "");

        private const string Css = @"
html,body{margin:0;height:100%;font-family:'Segoe UI',system-ui,sans-serif;background:#0e1524;color:#eaf0fb;overflow:hidden}
#bar{position:fixed;top:0;left:0;right:0;padding:8px 12px;background:#121b2d;border-bottom:1px solid #1e2942;font-size:13px;z-index:2}
#bar b{color:#eaf0fb}
#search{background:#0e1524;border:1px solid #1e2942;color:#eaf0fb;border-radius:6px;padding:4px 8px;width:180px}
#filters label{margin-right:8px;font-size:12px;color:#9aa7c2;cursor:pointer}
.legend{color:#9aa7c2;font-size:12px;margin-left:8px}
canvas{position:fixed;top:42px;left:0;display:block;cursor:grab}
";

        private const string Script = @"
const cv=document.getElementById('c'),ctx=cv.getContext('2d');
function resize(){cv.width=innerWidth;cv.height=innerHeight-42;}resize();addEventListener('resize',resize);
const COLORS={'Table':'#4d8bff','Form':'#12a150','View':'#22b8cf','Plugin Step':'#ff6b6b','Web Resource':'#f7871f','Workflow / Flow':'#a78bfa','Security Role':'#f472b6','Model-driven App':'#2dd4bf'};
function color(t){return COLORS[t]||'#8b95ad';}
const N=DATA.nodes,E=DATA.edges,idx={};
N.forEach((n,i)=>{idx[n.id]=n;n.x=cv.width/2+Math.cos(i)*200+Math.random()*40;n.y=cv.height/2+Math.sin(i)*200+Math.random()*40;n.vx=0;n.vy=0;});
const outAdj={},inAdj={};N.forEach(n=>{outAdj[n.id]=[];inAdj[n.id]=[];});
E.forEach(e=>{if(outAdj[e.s]&&inAdj[e.t]){outAdj[e.s].push(e.t);inAdj[e.t].push(e.s);}});
function reach(id,adj){const seen=new Set(),q=[id];while(q.length){const c=q.shift();(adj[c]||[]).forEach(x=>{if(!seen.has(x)){seen.add(x);q.push(x);}});}seen.delete(id);return seen;}
// type filters
const types=[...new Set(N.map(n=>n.type))].sort();const hidden=new Set();
const fdiv=document.getElementById('filters');
types.forEach(t=>{const l=document.createElement('label');const cb=document.createElement('input');cb.type='checkbox';cb.checked=true;cb.onchange=()=>{cb.checked?hidden.delete(t):hidden.add(t);};l.appendChild(cb);l.appendChild(document.createTextNode(' '+t));fdiv.appendChild(l);});
let sel=null,dep=new Set(),imp=new Set(),term='';
document.getElementById('search').addEventListener('input',e=>{term=e.target.value.toLowerCase();});
// physics
function step(){for(const n of N){n.vx*=0.85;n.vy*=0.85;}
 for(let i=0;i<N.length;i++)for(let j=i+1;j<N.length;j++){const a=N[i],b=N[j];let dx=a.x-b.x,dy=a.y-b.y,d=Math.sqrt(dx*dx+dy*dy)||1;const f=1400/(d*d);a.vx+=dx/d*f;a.vy+=dy/d*f;b.vx-=dx/d*f;b.vy-=dy/d*f;}
 for(const e of E){const a=idx[e.s],b=idx[e.t];if(!a||!b)continue;let dx=b.x-a.x,dy=b.y-a.y,d=Math.sqrt(dx*dx+dy*dy)||1;const f=(d-90)*0.01;a.vx+=dx/d*f;a.vy+=dy/d*f;b.vx-=dx/d*f;b.vy-=dy/d*f;}
 for(const n of N){if(n===drag)continue;n.x+=n.vx;n.y+=n.vy;const cx=cv.width/2,cy=cv.height/2;n.vx+=(cx-n.x)*0.0006;n.vy+=(cy-n.y)*0.0006;}}
function draw(){ctx.clearRect(0,0,cv.width,cv.height);
 ctx.strokeStyle='rgba(120,140,180,.25)';ctx.lineWidth=1;
 for(const e of E){const a=idx[e.s],b=idx[e.t];if(!a||!b)continue;if(hidden.has(a.type)||hidden.has(b.type))continue;
   ctx.strokeStyle=(sel&&(dep.has(e.t)&&e.s===sel||dep.has(e.s)&&dep.has(e.t)))?'rgba(18,161,80,.6)':(sel&&(imp.has(e.s)||imp.has(e.t)))?'rgba(209,52,56,.5)':'rgba(120,140,180,.22)';
   ctx.beginPath();ctx.moveTo(a.x,a.y);ctx.lineTo(b.x,b.y);ctx.stroke();}
 for(const n of N){if(hidden.has(n.type))continue;
   let r=6,c=color(n.type);
   if(sel){if(n.id===sel)r=9;else if(dep.has(n.id))c='#12a150';else if(imp.has(n.id))c='#d13438';else c='rgba(139,149,173,.35)';}
   if(term&&!(n.label||'').toLowerCase().includes(term))c='rgba(139,149,173,.2)';
   ctx.fillStyle=c;ctx.beginPath();ctx.arc(n.x,n.y,r,0,7);ctx.fill();
   if(r>6||term&&(n.label||'').toLowerCase().includes(term)){ctx.fillStyle='#eaf0fb';ctx.font='11px Segoe UI';ctx.fillText(n.label||'',n.x+10,n.y+4);}}}
function loop(){step();draw();requestAnimationFrame(loop);}loop();
// interaction
let drag=null,px,py;
function pick(mx,my){let best=null,bd=225;for(const n of N){if(hidden.has(n.type))continue;const dx=n.x-mx,dy=n.y-my,d=dx*dx+dy*dy;if(d<bd){bd=d;best=n;}}return best;}
cv.addEventListener('mousedown',e=>{const n=pick(e.offsetX,e.offsetY);if(n){drag=n;px=e.offsetX;py=e.offsetY;sel=n.id;dep=reach(n.id,outAdj);imp=reach(n.id,inAdj);}else{sel=null;}});
cv.addEventListener('mousemove',e=>{if(drag){drag.x=e.offsetX;drag.y=e.offsetY;drag.vx=0;drag.vy=0;}});
addEventListener('mouseup',()=>{drag=null;});
";
    }
}
