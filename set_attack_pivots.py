python3 <<'PY'
from pathlib import Path
import re

PIVOT_X = 0.8
PIVOT_Y = 0.5

files = [
    Path("Assets/Art/Characters/Character/1x atk_merged.png.meta"),
    Path("Assets/Art/Characters/Character/2x atk_merged(long).png.meta"),
    Path("Assets/Art/Characters/Character/3x atk_merged.png.meta"),
]

pivot_value = f"{{x: {PIVOT_X}, y: {PIVOT_Y}}}"

for meta in files:
    text = meta.read_text(encoding="utf-8", errors="ignore")
    new = text

    # múltiplos sprites dentro do sheet
    new = re.sub(r"(^\s*alignment:\s*)\d+", r"\g<1>9", new, flags=re.MULTILINE)
    new = re.sub(
        r"(^\s*pivot:\s*)\{x:\s*[-0-9.]+,\s*y:\s*[-0-9.]+\}",
        rf"\g<1>{pivot_value}",
        new,
        flags=re.MULTILINE,
    )

    # importer-level / single sprite fields
    new = re.sub(r"(^\s*spriteAlignment:\s*)\d+", r"\g<1>9", new, flags=re.MULTILINE)
    new = re.sub(
        r"(^\s*spritePivot:\s*)\{x:\s*[-0-9.]+,\s*y:\s*[-0-9.]+\}",
        rf"\g<1>{pivot_value}",
        new,
        flags=re.MULTILINE,
    )

    if new != text:
        meta.write_text(new, encoding="utf-8")
        print(f"OK: {meta}")
    else:
        print(f"SEM MUDANÇA: {meta}")
PY