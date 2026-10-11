"""Generate the interactive runtime diagram viewer from RuntimeFlow.md.

Requires network access to the pinned Mermaid CDN when opened.
"""

from pathlib import Path
import re, json


def main():
    """Write the browser viewer beside the source-linked Mermaid document."""
    repo = Path(__file__).resolve().parents[2]
    s = (repo / "Docs/RuntimeFlow.md").read_text(encoding="utf-8")
    sections = []
    for match in re.finditer(
        r"## ([^\n]+)\n\n(.*?)\n\n```mermaid\n(.*?)\n```", s, re.S
    ):
        sections.append(
            {"title": match[1], "description": match[2], "source": match[3]}
        )
    data = json.dumps(sections, ensure_ascii=False).replace("</", r"<\/")
    html = """<!doctype html>
    <html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
    <title>HY-Sandbox 运行时函数导航</title><link rel="icon" href="data:,"><style>
    *{box-sizing:border-box}body{margin:0;font-family:"Segoe UI","Microsoft YaHei",sans-serif;color:#e6edf5;background:#0e1624}
    header{padding:22px 28px;border-bottom:1px solid #304259;display:flex;justify-content:space-between;gap:24px;align-items:center}h1{font-size:23px;margin:0 0 8px}header p{margin:0;color:#aabbcc;font-size:14px}a{color:#73d1cf}main{display:grid;grid-template-columns:240px 1fr;height:calc(100vh - 100px)}nav{padding:16px;border-right:1px solid #304259;overflow:auto}button{background:#15243b;border:1px solid #344a64;border-radius:8px;color:#e6edf5;padding:11px;cursor:pointer;font:inherit}nav button{display:block;width:100%;text-align:left;margin-bottom:9px}button.active{background:#1a5358;border-color:#79d2c5}.content{display:flex;flex-direction:column;min-width:0}#description{padding:16px 24px;margin:0;color:#b9c9d9;line-height:1.65}.toolbar{display:flex;gap:10px;padding:0 24px 14px;align-items:center}.toolbar button{padding:6px 15px}#canvas{overflow:auto;flex:1;background:#f3f6fa;padding:30px;color:#17263e}#diagram{transform-origin:top left;min-width:750px}#diagram svg{max-width:none!important}#status{color:#86d4c9}details{padding:12px 24px}pre{white-space:pre-wrap;font-size:12px;max-height:170px;overflow:auto}@media(max-width:750px){main{grid-template-columns:150px 1fr}header{padding:18px}header>a{display:none}nav{padding:8px}nav button{font-size:12px}}
    </style></head><body><header><div><h1>HY-Sandbox · 运行时函数导航</h1><p>源码推导图：实线为直接调用，虚线标出协程、事件或注入动作。独立引擎回调不承诺执行先后。</p></div><a href="RuntimeFlow.md">可编辑 Mermaid 文档</a></header>
    <main><nav id="nav"></nav><section class="content"><p id="description"></p><div class="toolbar"><button id="minus" aria-label="缩小">−</button><span id="zoom">100%</span><button id="plus" aria-label="放大">+</button><button id="reset">重置</button><span id="status">载入图形…</span></div><div id="canvas"><div id="diagram"></div></div><details><summary>查看可编辑图源码</summary><pre id="source"></pre></details></section></main>
    <script type="module">
    import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11.12.0/dist/mermaid.esm.min.mjs';
    const diagrams=DATA;
    mermaid.initialize({startOnLoad:false,securityLevel:'loose',theme:'base',themeVariables:{primaryColor:'#dfeff0',primaryTextColor:'#17263e',primaryBorderColor:'#397681',lineColor:'#476880',fontFamily:'Segoe UI, Microsoft YaHei, sans-serif'},flowchart:{useMaxWidth:false,htmlLabels:true}});
    let selected=0,scale=1,revision=0;
    function zoom(){document.querySelector('#diagram').style.transform=`scale(${scale})`;document.querySelector('#zoom').textContent=Math.round(scale*100)+'%'}
    async function show(index){const generation=++revision; selected=index;document.querySelectorAll('nav button').forEach((b,i)=>b.classList.toggle('active',i===index));document.querySelector('#description').textContent=diagrams[index].description;document.querySelector('#source').textContent=diagrams[index].source;document.querySelector('#status').textContent='渲染…';try{const {svg,bindFunctions}=await mermaid.render('flow-'+generation,diagrams[index].source);if(generation!==revision)return;document.querySelector('#diagram').innerHTML=svg;bindFunctions?.(document.querySelector('#diagram'));scale=1;zoom();document.querySelector('#canvas').scrollTo(0,0);document.querySelector('#status').textContent='点击函数节点打开源码';document.body.dataset.rendered=String(index)}catch(e){document.querySelector('#status').textContent='渲染失败：'+e.message;document.body.dataset.error=e.message}}
    diagrams.forEach((d,i)=>{let b=document.createElement('button');b.textContent=d.title;b.addEventListener('click',()=>show(i));document.querySelector('#nav').append(b)});
    document.querySelector('#minus').onclick=()=>{scale=Math.max(.3,scale-.15);zoom()};document.querySelector('#plus').onclick=()=>{scale=Math.min(3,scale+.15);zoom()};document.querySelector('#reset').onclick=()=>{scale=1;zoom()};
    window.validateDiagrams=async()=>{for(const d of diagrams)await mermaid.parse(d.source);return diagrams.length};
    show(0);
    </script></body></html>""".replace("DATA", data)
    (repo / "Docs/RuntimeFlow.html").write_text(html, encoding="utf-8")
    print(len(sections), "interactive diagrams written; Mermaid CDN required")


if __name__ == "__main__":
    main()
