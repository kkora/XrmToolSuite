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
            sb.Append("<title>").Append(Html(title)).Append(" — Knowledge Graph</title>\n");
            sb.Append("<style>").Append(Css).Append("</style>\n");
            sb.Append("<div id=\"bar\">");
            sb.Append("<b>").Append(Html(title)).Append("</b> Knowledge Graph — ");
            sb.Append(g.NodeCount).Append(" nodes, ").Append(g.EdgeCount).Append(" edges. ");
            sb.Append("<input id=\"search\" placeholder=\"Search nodes…\"/> <button id=\"fit\" type=\"button\">Fit</button> <span id=\"filters\"></span>");
            sb.Append("<span class=\"legend\">Scroll = zoom · drag background = pan · click a node: <b style=\"color:#12a150\">green</b> = it depends on, <b style=\"color:#d13438\">red</b> = impacted by deleting it.</span>");
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
                    // Escape < and > so a label containing "</script>" can't terminate the embedding
                    // <script> block early (HTML tokenizes </script literally, ignoring JS string quoting).
                    case '<': sb.Append("\\u003c"); break;
                    case '>': sb.Append("\\u003e"); break;
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
#fit{background:#1b2942;border:1px solid #2b3a5c;color:#eaf0fb;border-radius:6px;padding:4px 10px;cursor:pointer;font-size:12px}
#fit:hover{background:#233457}
#filters label{margin-right:8px;font-size:12px;color:#9aa7c2;cursor:pointer}
.legend{color:#9aa7c2;font-size:12px;margin-left:8px}
canvas{position:fixed;top:0;left:0;display:block;cursor:grab}
";

        private const string Script = @"
const cv=document.getElementById('c'),ctx=cv.getContext('2d'),bar=document.getElementById('bar');
function barH(){return Math.ceil(bar.getBoundingClientRect().height);}
function resize(){cv.width=innerWidth;cv.height=Math.max(1,innerHeight-barH());cv.style.top=barH()+'px';}
resize();addEventListener('resize',resize);
const COLORS={'Table':'#4d8bff','Column':'#6aa9ff','Form':'#12a150','View':'#22b8cf','Option Set':'#eab308','Relationship':'#c084fc','Plugin Step':'#ff6b6b','Plugin Assembly':'#fb7185','Web Resource':'#f7871f','Workflow / Flow':'#a78bfa','Security Role':'#f472b6','Model-driven App':'#2dd4bf','Environment Variable':'#38bdf8','Environment Variable Value':'#7dd3fc','Site Map':'#facc15'};
function color(t){return COLORS[t]||'#8b95ad';}
const N=DATA.nodes,E=DATA.edges,idx={};
// Seed on a sunflower spiral around the world origin, sized to node count (layout is in world space,
// independent of the screen; the camera fits it into view).
const R=Math.max(240,Math.sqrt(N.length)*70);
N.forEach((n,i)=>{idx[n.id]=n;const a=i*2.399963229,rr=R*Math.sqrt((i+0.5)/N.length);n.x=Math.cos(a)*rr;n.y=Math.sin(a)*rr;n.vx=0;n.vy=0;});
const outAdj={},inAdj={};N.forEach(n=>{outAdj[n.id]=[];inAdj[n.id]=[];});
E.forEach(e=>{if(outAdj[e.s]&&inAdj[e.t]){outAdj[e.s].push(e.t);inAdj[e.t].push(e.s);}});
function reach(id,adj){const seen=new Set(),q=[id];while(q.length){const c=q.shift();(adj[c]||[]).forEach(x=>{if(!seen.has(x)){seen.add(x);q.push(x);}});}seen.delete(id);return seen;}
// type filters
const types=[...new Set(N.map(n=>n.type))].sort();const hidden=new Set();
const fdiv=document.getElementById('filters');
types.forEach(t=>{const l=document.createElement('label');const cb=document.createElement('input');cb.type='checkbox';cb.checked=true;cb.onchange=()=>{cb.checked?hidden.delete(t):hidden.add(t);};l.appendChild(cb);l.appendChild(document.createTextNode(' '+t));fdiv.appendChild(l);});
let sel=null,dep=new Set(),imp=new Set(),term='';
document.getElementById('search').addEventListener('input',e=>{term=e.target.value.toLowerCase();});
// camera: screen = world*view.s + view.t
const view={s:1,tx:0,ty:0};let userMoved=false;
function w2s(x,y){return [x*view.s+view.tx,y*view.s+view.ty];}
function s2w(x,y){return [(x-view.tx)/view.s,(y-view.ty)/view.s];}
function fit(){let any=false,minx=1e9,miny=1e9,maxx=-1e9,maxy=-1e9;for(const n of N){if(hidden.has(n.type))continue;any=true;if(n.x<minx)minx=n.x;if(n.y<miny)miny=n.y;if(n.x>maxx)maxx=n.x;if(n.y>maxy)maxy=n.y;}
 if(!any){view.s=1;view.tx=cv.width/2;view.ty=cv.height/2;return;}
 const w=Math.max(1,maxx-minx),h=Math.max(1,maxy-miny),pad=70;let s=Math.min((cv.width-2*pad)/w,(cv.height-2*pad)/h,2.5);if(!(s>0))s=1;
 view.s=s;view.tx=cv.width/2-(minx+maxx)/2*s;view.ty=cv.height/2-(miny+maxy)/2*s;}
document.getElementById('fit').addEventListener('click',()=>{userMoved=false;fit();});
// Interaction state — MUST be declared before loop() runs: step() reads `drag`, and a `let` read before
// its declaration is a ReferenceError (TDZ) that kills the render loop on the first frame (blank canvas).
let drag=null,pan=null;
// physics with cooling — repulsion is distance-capped and force-clamped so large graphs settle instead of
// exploding; alpha decays so motion stops. The camera auto-fits until the user pans/zooms/selects.
let alpha=1;
function step(){if(alpha<=0.02)return;alpha*=0.99;
 for(const n of N){n.vx*=0.9;n.vy*=0.9;}
 for(let i=0;i<N.length;i++){const a=N[i];for(let j=i+1;j<N.length;j++){const b=N[j];let dx=a.x-b.x,dy=a.y-b.y,d2=dx*dx+dy*dy;if(d2>160000)continue;if(d2<1)d2=1;const d=Math.sqrt(d2);let f=2600/d2;if(f>1.2)f=1.2;const ux=dx/d,uy=dy/d;a.vx+=ux*f;a.vy+=uy*f;b.vx-=ux*f;b.vy-=uy*f;}}
 for(const e of E){const a=idx[e.s],b=idx[e.t];if(!a||!b)continue;let dx=b.x-a.x,dy=b.y-a.y,d=Math.sqrt(dx*dx+dy*dy)||1;const f=(d-95)*0.015,ux=dx/d,uy=dy/d;a.vx+=ux*f;a.vy+=uy*f;b.vx-=ux*f;b.vy-=uy*f;}
 for(const n of N){if(n===drag)continue;n.vx+=-n.x*0.002;n.vy+=-n.y*0.002;n.x+=Math.max(-30,Math.min(30,n.vx))*alpha;n.y+=Math.max(-30,Math.min(30,n.vy))*alpha;}
 if(alpha>0.06&&!userMoved)fit();}
function draw(){ctx.setTransform(1,0,0,1,0,0);ctx.clearRect(0,0,cv.width,cv.height);ctx.lineWidth=1;
 for(const e of E){const a=idx[e.s],b=idx[e.t];if(!a||!b)continue;if(hidden.has(a.type)||hidden.has(b.type))continue;
   const p=w2s(a.x,a.y),q=w2s(b.x,b.y);
   ctx.strokeStyle=(sel&&((dep.has(e.t)&&e.s===sel)||(dep.has(e.s)&&dep.has(e.t))))?'rgba(18,161,80,.6)':(sel&&(imp.has(e.s)||imp.has(e.t)))?'rgba(209,52,56,.5)':'rgba(120,140,180,.22)';
   ctx.beginPath();ctx.moveTo(p[0],p[1]);ctx.lineTo(q[0],q[1]);ctx.stroke();}
 for(const n of N){if(hidden.has(n.type))continue;const p=w2s(n.x,n.y);if(p[0]<-40||p[1]<-40||p[0]>cv.width+40||p[1]>cv.height+40)continue;
   let r=6,c=color(n.type);
   if(sel){if(n.id===sel)r=9;else if(dep.has(n.id))c='#12a150';else if(imp.has(n.id))c='#d13438';else c='rgba(139,149,173,.35)';}
   const hit=term&&(n.label||'').toLowerCase().includes(term);
   if(term&&!hit)c='rgba(139,149,173,.2)';
   ctx.fillStyle=c;ctx.beginPath();ctx.arc(p[0],p[1],r,0,7);ctx.fill();
   // Always label nodes (dim by default, bright when selected/searched) so the graph never reads as empty.
   if(sel&&!(n.id===sel||dep.has(n.id)||imp.has(n.id)))continue;
   ctx.fillStyle=(r>6||hit)?'#eaf0fb':'rgba(190,200,220,.7)';ctx.font='11px Segoe UI';ctx.fillText(n.label||'',p[0]+9,p[1]+4);}}
function loop(){step();draw();requestAnimationFrame(loop);}
fit();loop();
// interaction: pick in screen space; drag a node, or drag the background to pan; wheel to zoom to cursor.
function pick(mx,my){let best=null,bd=256;for(const n of N){if(hidden.has(n.type))continue;const p=w2s(n.x,n.y),dx=p[0]-mx,dy=p[1]-my,d=dx*dx+dy*dy;if(d<bd){bd=d;best=n;}}return best;}
cv.addEventListener('mousedown',e=>{const n=pick(e.offsetX,e.offsetY);if(n){drag=n;sel=n.id;dep=reach(n.id,outAdj);imp=reach(n.id,inAdj);userMoved=true;}else{pan={x:e.offsetX,y:e.offsetY,tx:view.tx,ty:view.ty};sel=null;}});
cv.addEventListener('mousemove',e=>{if(drag){const w=s2w(e.offsetX,e.offsetY);drag.x=w[0];drag.y=w[1];drag.vx=0;drag.vy=0;}else if(pan){view.tx=pan.tx+(e.offsetX-pan.x);view.ty=pan.ty+(e.offsetY-pan.y);userMoved=true;}});
addEventListener('mouseup',()=>{drag=null;pan=null;});
cv.addEventListener('dblclick',()=>{userMoved=false;fit();});
cv.addEventListener('wheel',e=>{e.preventDefault();const w=s2w(e.offsetX,e.offsetY),k=e.deltaY<0?1.12:1/1.12;view.s=Math.max(0.05,Math.min(8,view.s*k));view.tx=e.offsetX-w[0]*view.s;view.ty=e.offsetY-w[1]*view.s;userMoved=true;},{passive:false});
";
    }
}
