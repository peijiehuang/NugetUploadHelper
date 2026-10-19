"""Compose an SVG poster from the real UI smoke-test screenshots (stdlib only)."""

import base64
from pathlib import Path
import shutil
import struct
from xml.etree import ElementTree
from xml.sax.saxutils import escape


def main():
    root = Path(__file__).resolve().parent.parent
    metadata = ElementTree.parse(root / "Directory.Build.props").getroot()
    version = escape(metadata.findtext("PropertyGroup/Version"))
    repository = escape(metadata.findtext("PropertyGroup/RepositoryUrl"))
    screenshots = root / "assets/screenshots"
    screenshots.mkdir(parents=True, exist_ok=True)
    for name in ("workbench.png", "uploaded.png"):
        shutil.copyfile(root / "artifacts/ui" / name, screenshots / name)

    screenshot = (screenshots / "workbench.png").read_bytes()
    width, height = struct.unpack(">II", screenshot[16:24])
    image_height = round(1792 * height / width)
    bottom = 340 + image_height
    poster_height = bottom + 180
    encoded = base64.b64encode(screenshot).decode("ascii")
    poster = f'''<svg xmlns="http://www.w3.org/2000/svg" width="1920" height="{poster_height}" viewBox="0 0 1920 {poster_height}" role="img" aria-labelledby="title description">
  <title id="title">NuGet 上传助手 v{version}</title>
  <desc id="description">真实程序截图：拖放导入、双认证、服务器版本比对与批量上传。截图使用演示数据。</desc>
  <rect x="1" y="1" width="1918" height="{poster_height - 2}" rx="24" fill="#ffffff" stroke="#dce3eb" stroke-width="2"/>
  <g font-family="Microsoft YaHei UI,Microsoft YaHei,Segoe UI,sans-serif">
    <rect x="64" y="61" width="8" height="25" rx="4" fill="#1264bd"/>
    <text x="88" y="82" font-size="23" fill="#64748b" letter-spacing="4">NUGET PACKAGE WORKBENCH</text>
    <text x="60" y="175" font-size="72" font-weight="700" fill="#202830">NuGet 上传助手</text>
    <text x="64" y="231" font-size="31" fill="#64748b">把 NuGet 包拖进来，先比版本，再批量上传。</text>
    <rect x="1604" y="106" width="252" height="64" rx="32" fill="#edf4ff" stroke="#d6e4f5"/>
    <text x="1730" y="148" text-anchor="middle" font-size="29" fill="#1264bd">v{version}</text>
    <g font-size="24" fill="#315678">
      <rect x="64" y="267" width="162" height="44" rx="22" fill="#eef4fb"/>
      <text x="145" y="297" text-anchor="middle">拖放导入</text>
      <rect x="242" y="267" width="162" height="44" rx="22" fill="#eef4fb"/>
      <text x="323" y="297" text-anchor="middle">版本比对</text>
      <rect x="420" y="267" width="252" height="44" rx="22" fill="#eef4fb"/>
      <text x="546" y="297" text-anchor="middle">API Key / 账号密码</text>
      <rect x="688" y="267" width="162" height="44" rx="22" fill="#eef4fb"/>
      <text x="769" y="297" text-anchor="middle">批量上传</text>
    </g>
    <rect x="56" y="332" width="1808" height="{image_height + 16}" rx="13" fill="#dce3eb"/>
    <image x="64" y="340" width="1792" height="{image_height}" href="data:image/png;base64,{encoded}"/>
    <rect x="64" y="{bottom + 40}" width="1792" height="2" fill="#e2e8f0"/>
    <text x="64" y="{bottom + 91}" font-size="26" fill="#24364b">Windows 10/11 x64</text>
    <text x="64" y="{bottom + 132}" font-size="23" fill="#64748b">自包含运行 · 无需命令行 · 真实程序截图 / 演示数据</text>
    <text x="1856" y="{bottom + 91}" text-anchor="end" font-size="25" fill="#315678">{repository.removeprefix('https://')}</text>
    <text x="1856" y="{bottom + 132}" text-anchor="end" font-size="23" fill="#64748b">peijiehuang / NugetUploadHelper</text>
  </g>
</svg>
'''
    destination = root / "assets/nuget-upload-helper-poster.svg"
    destination.write_text(poster, encoding="utf-8", newline="\n")
    print(destination)


if __name__ == "__main__":
    main()
