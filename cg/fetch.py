import requests

from bs4 import BeautifulSoup
import re


resp = requests.get(
    "https://en.wikipedia.org/wiki/List_of_HTTP_header_fields",
    proxies={"https": "http://127.0.0.1:7891"},
)
html = BeautifulSoup(resp.text, "lxml")

lst = []


def remove_link_ref(v: str) -> str:
    return re.sub(r"\[.*\]", "", v)


for tr in html.select("tr"):
    tds = tr.select("td")

    if len(tds) < 3:
        continue

    names: str
    names = tds[0].text.strip()
    names = list(map(lambda x: x.strip(), remove_link_ref(names).split(",")))

    obj = {
        "names": names,
        "desc": remove_link_ref(tds[1].text).strip(),
        "example": remove_link_ref(tds[2].text).strip(),
    }

    lst.append(obj)

buf = [
    """using System.Diagnostics;

namespace echo.primary.core.h2tp;

public enum RfcHeader{"""
]

ns: dict[str, str] = {}

for obj in lst:
    buf.append("/*<summary>")
    buf.append(obj["desc"])
    buf.append("</summary>\r\nExample:")
    buf.append(obj["example"])
    buf.append("*/")
    buf.append("")

    for name in obj["names"]:
        enum_name = name.replace("-", "")
        if enum_name in ns:
            continue
        ns[enum_name] = name

        buf.append(f"{enum_name},")

buf.append("}")

buf.append("internal static class HeaderToString{")
buf.append("static string ToString(Header ev) {")
buf.append("switch (ev) {")

for k, v in ns.items():
    buf.append(f'case Header.{k}: {{return "{v.lower()}";}}')

buf.append("default: { throw new UnreachableException();  }")
buf.append("}\r\n}\r\n}")

with open("./echo.primary/core/h2tp/Header.cs", "w", encoding="utf8") as f:
    f.write("\r\n".join(buf))
