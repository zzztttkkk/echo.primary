import requests
from bs4 import BeautifulSoup
import re


resp = requests.get(
    "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status",
    proxies={"https": "http://127.0.0.1:7891"},
)
html = BeautifulSoup(resp.text, "lxml")

lst: [str] = []


for code in html.select("div.section-content dt a code"):
    lst.append(code.text)

buf = [
    """using System.Diagnostics;

namespace echo.primary.core.h2tp;

public enum RfcStatusCode {"""
]

ns: dict[str, str] = {}

for obj in lst:
    obj: str
    idx = obj.find(" ")
    code = int(obj[:idx])
    raw_name = obj[idx + 1 :]
    name = raw_name.replace(" ", "").replace("-", "").replace("'", "")
    if name == "Imateapot":
        name = "IAmATeapot"

    if name == "unused":
        continue

    ns[name] = raw_name

    buf.append(f"{name}={code},")

buf.append("}")

buf.append("internal static class StatusToString{")
buf.append("static string ToString(RfcStatusCode ev) {")
buf.append("switch (ev) {")

for k, v in ns.items():
    buf.append(f'case RfcStatusCode.{k}: {{return "{v}";}}')

buf.append("default: { throw new UnreachableException();  }")
buf.append("}\r\n}\r\n}")


with open("./echo.primary/core/h2tp/StatusCode.cs", "w", encoding="utf8") as f:
    f.write("\r\n".join(buf))
