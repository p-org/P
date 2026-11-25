import Lake
open Lake DSL System

-- Verification-condition generation comes from Lean core (`Std.WP` /
-- `vcgen`); SMT discharge goes through lean-auto's SMT-LIB translation
-- (see `PLean/Verify/Smt.lean`). Mathlib is only used for `tauto` in
-- the fallback tactics (`set[T]` is PLean's own `PSet`).
require mathlib from git "https://github.com/leanprover-community/mathlib4" @ "v4.36.0"
require auto from git "https://github.com/leanprover-community/lean-auto.git" @ "v4.36.0"
require crush from git "https://github.com/AD1024/lean-crush" @ "bump/4.36.0"

package PLean where
  -- `experimental.intrinsic` acknowledges core's (experimental) loop
  -- `invariant` clauses, which PLean's `foreach` / `while` desugar to.
  leanOptions := #[⟨`pp.unicode.fun, true⟩, ⟨`experimental.intrinsic, true⟩]

-- Solver versions — must match dependencies.toml.
def z3.version := "4.15.4"
def cvc5.version := "1.3.4"
def dependencyFile := "dependencies.toml"

def z3.baseUrl := "https://github.com/Z3Prover/z3/releases/download"
def z3.arch := if System.Platform.target.startsWith "x86_64" then "x64" else "arm64"
def z3.platform :=
  if System.Platform.isWindows then "win"
  else if System.Platform.isOSX then "osx-13.7.6"
  else if System.Platform.target.startsWith "x86_64" then "glibc-2.39"
  else if System.Platform.target.startsWith "aarch64" then "glibc-2.34"
  else panic! s!"Unsupported platform: {System.Platform.target}"

def z3.target := s!"{arch}-{platform}"
def z3.fullName := s!"z3-{version}-{z3.target}"
def z3.url := s!"{baseUrl}/z3-{version}/{fullName}.zip"

def cvc5.baseUrl := "https://github.com/cvc5/cvc5/releases/download"
def cvc5.os :=
  if System.Platform.isWindows then "Win64"
  else if System.Platform.isOSX then "macOS"
  else "Linux"
def cvc5.arch := if System.Platform.target.startsWith "x86_64" then "x86_64" else "arm64"
def cvc5.target := s!"{os}-{arch}-static"
def cvc5.fullName := s!"cvc5-{cvc5.target}"
def cvc5.url := s!"{baseUrl}/cvc5-{version}/{fullName}.zip"

inductive Solver
| z3
| cvc5

instance : ToString Solver where
  toString := fun
    | Solver.z3 => "z3"
    | Solver.cvc5 => "cvc5"

def Solver.fullName (solver : Solver) : String :=
  match solver with
  | Solver.z3 => z3.fullName
  | Solver.cvc5 => cvc5.fullName

def Solver.url (solver : Solver) : String :=
  match solver with
  | Solver.z3 => z3.url
  | Solver.cvc5 => cvc5.url

def Lake.unzip (file : FilePath) (dir : FilePath) : LogIO PUnit := do
  IO.FS.createDirAll dir
  proc (quiet := true) {
    cmd := "unzip"
    args := #["-d", dir.toString, file.toString]
  }

def copyFile' (src : FilePath) (dst : FilePath) : LogIO PUnit := do
  proc (quiet := true) {
    cmd := "cp"
    args := #[src.toString, dst.toString]
  }

def downloadSolver (solver : Solver) (pkg : Package) (oFile : FilePath) : JobM PUnit := do
  let zipPath := (pkg.buildDir / s!"{solver}").addExtension "zip"
  logVerbose s!"Downloading {solver} from {solver.url}"
  download solver.url zipPath
  let extractedPath := pkg.buildDir / solver.fullName
  if ← extractedPath.pathExists then
    IO.FS.removeDirAll extractedPath
  unzip zipPath pkg.buildDir
  let binPath := extractedPath / "bin" / s!"{solver}"
  IO.FS.createDirAll oFile.parent.get!
  copyFile' binPath oFile
  if ← oFile.pathExists then
    logVerbose s!"{solver} is now at {oFile}"
  else
    logError s!"Failed to download {solver} to {oFile}"
  IO.FS.removeFile zipPath
  IO.FS.removeDirAll extractedPath

def downloadDependency (pkg : Package) (solver : Solver) (oFile : FilePath) := do
  let lakefilePath := pkg.lakeDir / ".." / dependencyFile
  let srcJob ← inputTextFile lakefilePath
  buildFileAfterDep oFile srcJob (fun _srcFile => downloadSolver solver pkg oFile)
    (extraDepTrace := pure (.ofHash (Hash.ofString solver.url) s!"{solver} url: {solver.url}"))

-- Solvers are installed into `<PLean>/.lake/build/bin`.
target downloadDependencies pkg : Array FilePath := do
  let solverDir := pkg.buildDir / "bin"
  let z3 ← downloadDependency pkg Solver.z3 (solverDir / "z3")
  let cvc5 ← downloadDependency pkg Solver.cvc5 (solverDir / "cvc5")
  return Job.collectArray #[z3, cvc5]

@[default_target]
lean_lib PLean where
  globs := #[Glob.one `PLean, Glob.submodules `PLean]
  extraDepTargets := #[``downloadDependencies]

lean_lib Examples where
  globs := #[Glob.submodules `Examples]
  extraDepTargets := #[``downloadDependencies]

lean_lib Tests where
  globs := #[Glob.submodules `Tests]
  extraDepTargets := #[``downloadDependencies]
