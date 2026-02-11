#!/usr/bin/env python3
"""
PChatBot Command Line Interface.

A CLI for generating, compiling, and verifying P code using the service layer.

Usage:
    python -m ui.cli.app generate --design-doc path/to/doc.txt --output path/to/project
    python -m ui.cli.app compile path/to/project
    python -m ui.cli.app check path/to/project --schedules 100 --timeout 60
    python -m ui.cli.app fix path/to/project --error "error message"
"""

import argparse
import sys
import os
from pathlib import Path
from typing import Optional

# Add project paths
PROJECT_ROOT = Path(__file__).parent.parent.parent.parent
SRC_ROOT = Path(__file__).parent.parent.parent

if str(SRC_ROOT) not in sys.path:
    sys.path.insert(0, str(SRC_ROOT))
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

# Load environment
from dotenv import load_dotenv
load_dotenv(PROJECT_ROOT / ".env", override=True)

from core.workflow import (
    WorkflowEngine,
    WorkflowFactory,
    EventEmitter,
    LoggingEventListener,
    extract_machine_names_from_design_doc,
)
from core.services import (
    GenerationService,
    CompilationService,
    FixerService,
)
from core.services.base import ResourceLoader
from core.llm import get_default_provider


class PChatBotCLI:
    """Command-line interface for PChatBot."""
    
    def __init__(self, verbose: bool = False):
        self.verbose = verbose
        self._initialized = False
        self._engine: Optional[WorkflowEngine] = None
        self._factory: Optional[WorkflowFactory] = None
        self._services = {}
    
    def _ensure_initialized(self):
        """Lazy initialization of services."""
        if self._initialized:
            return
        
        print("🔧 Initializing PChatBot...")
        
        # Get LLM provider
        provider = get_default_provider()
        print(f"   Using LLM provider: {provider.name}")
        
        # Create resource loader
        resource_loader = ResourceLoader(PROJECT_ROOT / "resources")
        
        # Create services
        self._services["generation"] = GenerationService(
            llm_provider=provider,
            resource_loader=resource_loader
        )
        self._services["compilation"] = CompilationService(
            llm_provider=provider,
            resource_loader=resource_loader
        )
        self._services["fixer"] = FixerService(
            llm_provider=provider,
            resource_loader=resource_loader,
            compilation_service=self._services["compilation"]
        )
        
        # Create event emitter with logging
        emitter = EventEmitter()
        emitter.on_all(LoggingEventListener(verbose=self.verbose))
        
        # Create engine and factory
        self._engine = WorkflowEngine(emitter)
        self._factory = WorkflowFactory(
            generation_service=self._services["generation"],
            compilation_service=self._services["compilation"],
            fixer_service=self._services["fixer"]
        )
        
        # Register standard workflows
        self._engine.register_workflow(
            self._factory.create_compile_and_fix_workflow()
        )
        self._engine.register_workflow(
            self._factory.create_full_verification_workflow()
        )
        self._engine.register_workflow(
            self._factory.create_quick_check_workflow()
        )
        
        self._initialized = True
        print("✅ Initialization complete\n")
    
    def generate(
        self,
        design_doc_path: str,
        output_path: str,
        machine_names: Optional[list] = None,
        save_files: bool = True
    ) -> int:
        """
        Generate P code from a design document.
        
        Args:
            design_doc_path: Path to the design document
            output_path: Where to save the generated project
            machine_names: Optional list of machine names
            save_files: Whether to save files to disk
            
        Returns:
            Exit code (0 for success, 1 for error)
        """
        self._ensure_initialized()
        
        # Read design doc
        print(f"📄 Reading design document: {design_doc_path}")
        try:
            with open(design_doc_path, 'r') as f:
                design_doc = f.read()
        except FileNotFoundError:
            print(f"❌ File not found: {design_doc_path}")
            return 1
        except Exception as e:
            print(f"❌ Error reading file: {e}")
            return 1
        
        # Extract machine names if not provided
        if not machine_names:
            machine_names = extract_machine_names_from_design_doc(design_doc)
            if not machine_names:
                print("❌ Could not extract machine names from design document.")
                print("   Please provide them with --machines Machine1,Machine2")
                return 1
            print(f"   Extracted machines: {', '.join(machine_names)}")
        
        # Create output directory
        os.makedirs(output_path, exist_ok=True)
        print(f"📁 Output directory: {output_path}")
        
        # Create and run workflow
        workflow = self._factory.create_full_generation_workflow(machine_names)
        self._engine.register_workflow(workflow)
        
        result = self._engine.execute("full_generation", {
            "design_doc": design_doc,
            "project_path": output_path
        })
        
        # Handle results
        if result.get("success"):
            print(f"\n✅ Project generated successfully!")
            print(f"   Location: {output_path}")
            
            # List generated files
            if save_files:
                completed = result.get("completed_steps", [])
                print(f"   Completed steps: {len(completed)}")
            
            return 0
        else:
            print(f"\n❌ Generation failed!")
            errors = result.get("errors", [])
            for error in errors:
                print(f"   • {error}")
            
            # Check if needs guidance
            if result.get("needs_guidance"):
                print(f"\n⚠️ Human guidance needed:")
                print(f"   {result.get('guidance_context', 'Unknown')}")
            
            return 1
    
    def compile(self, project_path: str, fix_errors: bool = True) -> int:
        """
        Compile a P project.
        
        Args:
            project_path: Path to the P project
            fix_errors: Whether to attempt fixing errors
            
        Returns:
            Exit code
        """
        self._ensure_initialized()
        
        print(f"🔨 Compiling project: {project_path}")
        
        if not os.path.exists(project_path):
            print(f"❌ Project path not found: {project_path}")
            return 1
        
        workflow_name = "compile_and_fix" if fix_errors else "compile_only"
        
        # Use compile_and_fix workflow
        result = self._engine.execute("compile_and_fix", {
            "project_path": project_path
        })
        
        if result.get("success"):
            print(f"\n✅ Compilation successful!")
            return 0
        else:
            print(f"\n❌ Compilation failed!")
            for error in result.get("errors", []):
                print(f"   • {error}")
            return 1
    
    def check(
        self,
        project_path: str,
        schedules: int = 100,
        timeout: int = 60
    ) -> int:
        """
        Run PChecker on a P project.
        
        Args:
            project_path: Path to the P project
            schedules: Number of schedules
            timeout: Timeout in seconds
            
        Returns:
            Exit code
        """
        self._ensure_initialized()
        
        print(f"🔍 Running PChecker: {project_path}")
        print(f"   Schedules: {schedules}, Timeout: {timeout}s")
        
        if not os.path.exists(project_path):
            print(f"❌ Project path not found: {project_path}")
            return 1
        
        result = self._services["compilation"].run_checker(
            project_path=project_path,
            schedules=schedules,
            timeout=timeout
        )
        
        if result.success:
            print(f"\n✅ PChecker passed!")
            if self.verbose and result.output:
                print(f"\nOutput:\n{result.output}")
            return 0
        else:
            print(f"\n❌ PChecker found errors!")
            if result.output:
                print(f"\n{result.output}")
            return 1
    
    def fix(
        self,
        project_path: str,
        error_message: Optional[str] = None,
        file_path: Optional[str] = None,
        max_iterations: int = 10
    ) -> int:
        """
        Fix compilation errors in a P project.
        
        Args:
            project_path: Path to the P project
            error_message: Optional specific error to fix
            file_path: Optional file with the error
            max_iterations: Maximum fix attempts
            
        Returns:
            Exit code
        """
        self._ensure_initialized()
        
        print(f"🔧 Fixing errors in: {project_path}")
        
        if not os.path.exists(project_path):
            print(f"❌ Project path not found: {project_path}")
            return 1
        
        result = self._services["fixer"].fix_iteratively(
            project_path=project_path,
            max_iterations=max_iterations
        )
        
        if result.success:
            print(f"\n✅ Errors fixed!")
            if result.fixes_applied:
                print(f"   Fixes applied: {result.fixes_applied}")
            return 0
        elif result.needs_guidance:
            print(f"\n⚠️ Could not fix automatically.")
            print(f"   Guidance needed: {result.guidance_questions}")
            return 2
        else:
            print(f"\n❌ Failed to fix errors!")
            if result.error:
                print(f"   {result.error}")
            return 1
    
    def list_workflows(self) -> int:
        """List available workflows."""
        self._ensure_initialized()
        
        print("📋 Available Workflows:\n")
        
        workflows = [
            ("full_generation", "Generate complete P project from design document"),
            ("compile_and_fix", "Compile project and fix errors"),
            ("full_verification", "Compile, fix, and run PChecker"),
            ("quick_check", "Run PChecker only"),
        ]
        
        for name, desc in workflows:
            print(f"  • {name}")
            print(f"    {desc}\n")
        
        return 0


def main():
    """Main entry point for the CLI."""
    parser = argparse.ArgumentParser(
        description="PChatBot - AI-powered P code generation and verification",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  Generate P code from design doc:
    python -m ui.cli.app generate --design-doc design.txt --output ./my_project

  Compile a P project:
    python -m ui.cli.app compile ./my_project

  Run PChecker:
    python -m ui.cli.app check ./my_project --schedules 100 --timeout 60

  Fix compilation errors:
    python -m ui.cli.app fix ./my_project

  List available workflows:
    python -m ui.cli.app workflows
        """
    )
    
    parser.add_argument(
        "-v", "--verbose",
        action="store_true",
        help="Enable verbose output"
    )
    
    subparsers = parser.add_subparsers(dest="command", help="Command to run")
    
    # Generate command
    gen_parser = subparsers.add_parser("generate", help="Generate P code from design doc")
    gen_parser.add_argument(
        "-d", "--design-doc",
        required=True,
        help="Path to design document"
    )
    gen_parser.add_argument(
        "-o", "--output",
        required=True,
        help="Output directory for generated project"
    )
    gen_parser.add_argument(
        "-m", "--machines",
        help="Comma-separated list of machine names (auto-extracted if not provided)"
    )
    gen_parser.add_argument(
        "--no-save",
        action="store_true",
        help="Don't save files (preview only)"
    )
    
    # Compile command
    compile_parser = subparsers.add_parser("compile", help="Compile a P project")
    compile_parser.add_argument(
        "project_path",
        help="Path to the P project"
    )
    compile_parser.add_argument(
        "--no-fix",
        action="store_true",
        help="Don't attempt to fix errors"
    )
    
    # Check command
    check_parser = subparsers.add_parser("check", help="Run PChecker on a project")
    check_parser.add_argument(
        "project_path",
        help="Path to the P project"
    )
    check_parser.add_argument(
        "-s", "--schedules",
        type=int,
        default=100,
        help="Number of schedules (default: 100)"
    )
    check_parser.add_argument(
        "-t", "--timeout",
        type=int,
        default=60,
        help="Timeout in seconds (default: 60)"
    )
    
    # Fix command
    fix_parser = subparsers.add_parser("fix", help="Fix compilation errors")
    fix_parser.add_argument(
        "project_path",
        help="Path to the P project"
    )
    fix_parser.add_argument(
        "-e", "--error",
        help="Specific error message to fix"
    )
    fix_parser.add_argument(
        "-f", "--file",
        help="File containing the error"
    )
    fix_parser.add_argument(
        "-i", "--iterations",
        type=int,
        default=10,
        help="Maximum fix iterations (default: 10)"
    )
    
    # Workflows command
    subparsers.add_parser("workflows", help="List available workflows")
    
    args = parser.parse_args()
    
    if not args.command:
        parser.print_help()
        return 0
    
    cli = PChatBotCLI(verbose=args.verbose)
    
    if args.command == "generate":
        machines = args.machines.split(",") if args.machines else None
        return cli.generate(
            design_doc_path=args.design_doc,
            output_path=args.output,
            machine_names=machines,
            save_files=not args.no_save
        )
    
    elif args.command == "compile":
        return cli.compile(
            project_path=args.project_path,
            fix_errors=not args.no_fix
        )
    
    elif args.command == "check":
        return cli.check(
            project_path=args.project_path,
            schedules=args.schedules,
            timeout=args.timeout
        )
    
    elif args.command == "fix":
        return cli.fix(
            project_path=args.project_path,
            error_message=args.error,
            file_path=args.file,
            max_iterations=args.iterations
        )
    
    elif args.command == "workflows":
        return cli.list_workflows()
    
    return 0


if __name__ == "__main__":
    sys.exit(main())
