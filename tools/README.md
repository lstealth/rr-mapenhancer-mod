This folder contains helper scripts used by the project.

get-managed-assembly-path.ps1
- Scans upward from the current working directory to find `Directory.Build.props`.
- Reads `RrInstallDir` and `RrManagedDir`, expands referenced properties, and prints the full path to `Assembly-CSharp.dll`.
- Supports `-CopyTo <dir>` to copy the assembly into a local folder (useful for tooling that needs the file).
