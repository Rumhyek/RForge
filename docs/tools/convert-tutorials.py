#!/usr/bin/env python3
"""Convert a library's tutorial markdown into the docs site's Razor pages.

    python docs/tools/convert-tutorials.py events            # write the pages
    python docs/tools/convert-tutorials.py autoapi --check   # fail if the pages differ from the markdown

The markdown in docs/<library>/tutorials is the source of truth for the text. The pages are generated, so
change the markdown and run this script. It supports only the markdown the tutorials use: headings, paragraphs,
flat lists, tables, block quotes, fenced code, and inline code, bold, italic and links. Anything else is an error,
so a new construct has to be added here on purpose.

Standard library only.
"""
import argparse
import difflib
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
PAGES = REPO / "docs/RForgeDocs/RForgeDocs/RForgeDocs/Components/Pages"
GITHUB = "https://github.com/Rumhyek/RForge"

LIBRARIES = {
    "events": {
        "markdown": REPO / "docs/events/tutorials",
        "pages": PAGES / "EventsTutorials",
        "set": "Events",
        "route": "/events/tutorials",
        # Links from a tutorial to the design docs and the samples go to GitHub.
        "link_rules": [
            (r"^\.\./design/([\w.-]+\.md)(#.*)?$", GITHUB + r"/blob/main/docs/events/design/\1\2"),
            (r"^\.\./\.\./\.\./samples$", GITHUB + "/tree/main/samples"),
        ],
        # The sample pages became example pages on the docs site.
        "example_rule": (r"^/examples/([\w-]+)$", r"/events/example/\1"),
    },
    "autoapi": {
        "markdown": REPO / "docs/autoapi/tutorials",
        "pages": PAGES / "AutoApiTutorials",
        "set": "AutoApi",
        "route": "/autoapi/tutorials",
        "link_rules": [
            (r"^\.\./spec\.md(#.*)?$", GITHUB + r"/blob/main/docs/autoapi/spec.md\1"),
            (r"^\.\./\.\./\.\./samples$", GITHUB + "/tree/main/samples"),
        ],
        "example_rule": None,
    },
}


class ConvertError(Exception):
    pass


def escape(text):
    """HTML-escape text and double the @ so Razor doesn't read it as code."""
    return text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;").replace("@", "@@")


def github_slug(heading):
    """The id GitHub gives a heading, so anchors written for the markdown keep working."""
    text = re.sub(r"`", "", heading).lower()
    text = re.sub(r"[^\w\- ]", "", text)
    return text.replace(" ", "-")


class Converter:
    def __init__(self, config, slugs):
        self.config = config
        self.slugs = slugs

    # ---- links -----------------------------------------------------------------------------------------------

    def map_link(self, url):
        for pattern, replacement in self.config["link_rules"]:
            if re.match(pattern, url):
                return re.sub(pattern, replacement, url)
        match = re.match(r"^([\w-]+)\.md(#.*)?$", url)
        if match:
            if match.group(1) == "README":
                return self.config["route"]
            if match.group(1) not in self.slugs:
                raise ConvertError(f"link to an unknown tutorial: {url}")
            return f"{self.config['route']}/{match.group(1)}{match.group(2) or ''}"
        return url

    # ---- inline ----------------------------------------------------------------------------------------------

    def inline(self, text):
        spans = []

        def keep_code(match):
            code = match.group(1)
            rule = self.config["example_rule"]
            html = f"<code>{escape(code)}</code>"
            if rule and re.match(rule[0], code):
                target = re.sub(rule[0], rule[1], code)
                html = f'<a href="{target}"><code>{escape(target)}</code></a>'
            elif re.match(r"^/(events|autoapi)/example/[\w-]+$", code):
                html = f'<a href="{code}"><code>{escape(code)}</code></a>'
            spans.append(html)
            return f"\x00{len(spans) - 1}\x00"

        text = re.sub(r"`([^`]+)`", keep_code, text)
        text = escape(text)
        text = re.sub(r"\[([^\]]+)\]\(([^)\s]+)\)", lambda m: f'<a href="{self.map_link(m.group(2).replace("&amp;", "&"))}">{m.group(1)}</a>', text)
        text = re.sub(r"\*\*(.+?)\*\*", r"<strong>\1</strong>", text)
        text = re.sub(r"(?<![\w*])\*([^*\s][^*]*?)\*(?![\w*])", r"<em>\1</em>", text)
        text = re.sub(r"\x00(\d+)\x00", lambda m: spans[int(m.group(1))], text)
        return text

    # ---- blocks ----------------------------------------------------------------------------------------------

    def convert(self, markdown):
        lines = markdown.replace("\r\n", "\n").split("\n")
        out = []
        ids = {}
        i = 0
        seen_h1 = False

        def flush_paragraph(buffer):
            if buffer:
                out.append(f"<p>{self.inline(' '.join(buffer))}</p>")
                buffer.clear()

        paragraph = []
        while i < len(lines):
            line = lines[i]

            if line.startswith("```"):
                flush_paragraph(paragraph)
                i += 1
                code = []
                while i < len(lines) and not lines[i].startswith("```"):
                    code.append(lines[i])
                    i += 1
                if i >= len(lines):
                    raise ConvertError("unterminated code fence")
                i += 1
                out.append("<pre><code>" + escape("\n".join(code)) + "</code></pre>")
                continue

            if not line.strip():
                flush_paragraph(paragraph)
                i += 1
                continue

            heading = re.match(r"^(#{1,4}) (.+)$", line)
            if heading:
                flush_paragraph(paragraph)
                level, title = len(heading.group(1)), heading.group(2).strip()
                i += 1
                if level == 1:
                    if seen_h1:
                        raise ConvertError(f"second H1: {title}")
                    seen_h1 = True  # The page hero shows the title.
                    continue
                slug = github_slug(title)
                count = ids.get(slug, 0)
                ids[slug] = count + 1
                if count:
                    slug = f"{slug}-{count}"
                out.append(f'<h{level} id="{slug}">{self.inline(title)}</h{level}>')
                continue

            if line.startswith("|"):
                flush_paragraph(paragraph)
                rows = []
                while i < len(lines) and lines[i].startswith("|"):
                    rows.append(lines[i])
                    i += 1
                out.append(self.table(rows))
                continue

            if line.startswith(">"):
                flush_paragraph(paragraph)
                quote = []
                while i < len(lines) and lines[i].startswith(">"):
                    quote.append(lines[i][1:].strip())
                    i += 1
                out.append("<blockquote>\n<p>" + self.inline(" ".join(quote)) + "</p>\n</blockquote>")
                continue

            list_item = re.match(r"^(-|\d+\.) (.+)$", line)
            if list_item:
                flush_paragraph(paragraph)
                ordered = list_item.group(1) != "-"
                items = []
                while i < len(lines):
                    current = re.match(r"^(-|\d+\.) (.+)$", lines[i])
                    if current and (current.group(1) != "-") == ordered:
                        items.append([current.group(2)])
                        i += 1
                    elif lines[i].startswith("  ") and lines[i].strip() and items:
                        if re.match(r"^\s+(-|\d+\.) ", lines[i]):
                            raise ConvertError(f"nested list is not supported: {lines[i].strip()}")
                        items[-1].append(lines[i].strip())
                        i += 1
                    else:
                        break
                tag = "ol" if ordered else "ul"
                body = "\n".join(f"<li>{self.inline(' '.join(item))}</li>" for item in items)
                out.append(f"<{tag}>\n{body}\n</{tag}>")
                continue

            if line.startswith("<") or line.startswith("---"):
                raise ConvertError(f"raw HTML or a rule is not supported: {line}")

            paragraph.append(line.strip())
            i += 1

        flush_paragraph(paragraph)
        return "\n\n".join(out)

    def table(self, rows):
        def cells(row):
            parts = re.split(r"(?<!\\)\|", row.strip())[1:-1]
            return [c.strip().replace("\\|", "|") for c in parts]

        if len(rows) < 3 or not re.match(r"^\|[\s:|-]+\|$", rows[1].strip()):
            raise ConvertError("table without a header separator")
        head = "".join(f"<th>{self.inline(c)}</th>" for c in cells(rows[0]))
        body = []
        for row in rows[2:]:
            body.append("<tr>" + "".join(f"<td>{self.inline(c)}</td>" for c in cells(row)) + "</tr>")
        return (
            '<div class="table-container"><table class="table is-striped">\n'
            f"<thead>\n<tr>{head}</tr>\n</thead>\n<tbody>\n" + "\n".join(body) + "\n</tbody>\n</table></div>"
        )


def page_name(slug):
    number, _, rest = slug.partition("-")
    return "Tutorial" + number + "".join(word.capitalize() for word in rest.split("-")) + ".razor"


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("library", choices=sorted(LIBRARIES))
    parser.add_argument("--check", action="store_true", help="compare with the existing pages instead of writing them")
    args = parser.parse_args()

    config = LIBRARIES[args.library]
    sources = sorted(p for p in config["markdown"].glob("[0-9][0-9]-*.md"))
    if not sources:
        sys.exit(f"no tutorials found in {config['markdown']}")
    converter = Converter(config, {p.stem for p in sources})

    differences = 0
    for source in sources:
        try:
            body = converter.convert(source.read_text(encoding="utf-8"))
        except ConvertError as error:
            sys.exit(f"{source.name}: {error}")
        page = (
            f'@page "{config["route"]}/{source.stem}"\n\n'
            f'<TutorialDetail Tutorials="TutorialSet.{config["set"]}" Slug="{source.stem}">\n'
            f"{body}\n</TutorialDetail>\n"
        )
        target = config["pages"] / page_name(source.stem)
        if args.check:
            current = target.read_text(encoding="utf-8").replace("\r\n", "\n") if target.exists() else ""
            if current != page:
                differences += 1
                print(f"--- {target.name} differs from {source.name}")
                diff = difflib.unified_diff(current.splitlines(), page.splitlines(), "page", "converted", lineterm="", n=0)
                print("\n".join(list(diff)[:40]))
        else:
            config["pages"].mkdir(parents=True, exist_ok=True)
            target.write_text(page, encoding="utf-8", newline="\n")
            print(f"wrote {target.relative_to(REPO)}")

    if args.check:
        print(f"{len(sources)} tutorials checked, {differences} differ")
        sys.exit(1 if differences else 0)


if __name__ == "__main__":
    main()
