"""Check serialized generated mesh index buffers without requiring Unity or packages."""
from pathlib import Path
import re
import struct

root = Path(__file__).resolve().parents[1]
paths = sorted((root / "Assets/App/Art").glob("*/*.asset"))
rows = []
for path in paths:
    source = path.read_text(encoding="utf-8")
    if "\nMesh:" not in source:
        continue
    vertex_count = int(re.search(r"m_VertexCount: (\d+)", source)[1])
    index_format = int(re.search(r"m_IndexFormat: (\d+)", source)[1])
    buffer = bytes.fromhex(re.search(r"m_IndexBuffer: (\w+)", source)[1])
    width, code = (4, "I") if index_format else (2, "H")
    indices = struct.unpack("<" + code * (len(buffer) // width), buffer)
    invalid = sum(index >= vertex_count for index in indices)
    passed = vertex_count > 0 and len(indices) > 0 and not invalid
    rows.append(f"{'PASS' if passed else 'FAIL'} {path.stem}: vertices={vertex_count}, triangles={len(indices)//3}, invalid_indices={invalid}")
rows.append(f"TOTAL {len(rows)} meshes / {sum(row.startswith('FAIL') for row in rows)} failures")
report = root / "VisualReview/mesh-validation.txt"
report.write_text("\n".join(rows) + "\n", encoding="utf-8")
print("\n".join(rows))
raise SystemExit(1 if any(row.startswith("FAIL") for row in rows) else 0)
