"""Check offline tool entrypoints and syntax without executing asset or save writers.

Run from any directory: python Tools/CodeReadability/check_tool_sources.py.
C# eval_file syntax/format uses the companion --check-tools command.
"""

import ast
from pathlib import Path


def check_python(path):
    """Compile in memory; require a documented, explicit side-effect entrypoint."""
    source = path.read_text(encoding="utf-8")
    tree = ast.parse(source, filename=str(path))
    compile(tree, str(path), "exec")
    assert "职责" in source[:500] or ast.get_docstring(tree), path
    assert any(
        isinstance(node, ast.FunctionDef) and node.name == "main" for node in tree.body
    ), path
    assert isinstance(tree.body[-1], ast.If), path
    assert ast.unparse(tree.body[-1].test) == "__name__ == '__main__'", path
    # A top-level expression must be a module docstring, never an asset-writing call.
    for node in tree.body[:-1]:
        if isinstance(node, ast.Expr):
            assert isinstance(node.value, ast.Constant) and isinstance(
                node.value.value, str
            ), path


def check_generator_templates(repo):
    """Keep formatted eval_file source and its generator boundaries in sync."""
    path = repo / "Blueprints/DSH_Enemies/write_generator.py"
    tree = ast.parse(path.read_text(encoding="utf-8"))
    templates = {
        node.targets[0].id: ast.literal_eval(node.value)
        for node in tree.body
        if isinstance(node, ast.Assign)
        and isinstance(node.targets[0], ast.Name)
        and node.targets[0].id in ("HEADER", "FOOTER")
    }
    generated = path.with_name("Generate.cs").read_text(encoding="utf-8")
    assert generated.startswith(templates["HEADER"]), "Generator header differs"
    assert generated.endswith(templates["FOOTER"]), "Generator footer differs"


def main():
    """Report static checks only; do not import or run the checked tools."""
    repo = Path(__file__).resolve().parents[2]
    roots = (
        repo / "ArtSource/VfxGraph",
        repo / "Blueprints/DSH_Enemies",
        Path(__file__).parent,
    )
    paths = sorted(path for root in roots for path in root.glob("*.py"))
    for path in paths:
        check_python(path)
    check_generator_templates(repo)
    print(
        f"Tool Python syntax/entrypoints: {len(paths)} passed; generator templates synchronized"
    )


if __name__ == "__main__":
    main()
