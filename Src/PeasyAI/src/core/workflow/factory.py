"""
Workflow Factory for PeasyAI.

This module provides functionality to create workflows from YAML configuration
and from design documents. It handles:
- Loading workflow definitions from YAML
- Creating step instances with proper services
- Extracting machine names from design documents
"""

import os
import re
from typing import Any, Dict, List, Optional
import yaml

from .steps import WorkflowStep, CompositeStep
from .engine import WorkflowEngine, WorkflowDefinition
from .events import EventEmitter
from .p_steps import (
    CreateProjectStructureStep,
    GenerateTypesEventsStep,
    GenerateMachineStep,
    GenerateSpecStep,
    GenerateTestStep,
    SaveGeneratedFilesStep,
    CompileProjectStep,
    FixCompilationErrorsStep,
    RunCheckerStep,
    FixCheckerErrorsStep,
)
from ..services.generation import GenerationService
from ..services.compilation import CompilationService
from ..services.fixer import FixerService


class WorkflowFactory:
    """
    Factory for creating workflows.
    
    Creates workflow instances with proper service dependencies
    and step configurations.
    
    Usage:
        factory = WorkflowFactory(generation_service, compilation_service, fixer_service)
        
        # Create from design doc
        workflow = factory.create_full_generation_workflow(
            design_doc="...",
            machine_names=["Client", "Server"]
        )
        
        # Or load from config
        workflows = factory.load_from_yaml("configuration/workflows.yaml")
    """
    
    def __init__(
        self,
        generation_service: GenerationService,
        compilation_service: CompilationService,
        fixer_service: FixerService
    ):
        self.generation_service = generation_service
        self.compilation_service = compilation_service
        self.fixer_service = fixer_service
    
    def create_full_generation_workflow(
        self,
        machine_names: List[str],
        spec_name: str = "Safety",
        test_name: str = "TestDriver",
        ensemble_size: int = 3,
    ) -> WorkflowDefinition:
        """
        Create a full project generation workflow.
        
        Args:
            machine_names: List of machine names to generate
            spec_name: Name for the specification file
            test_name: Name for the test file
            ensemble_size: Number of candidates per file for ensemble selection
            
        Returns:
            WorkflowDefinition ready for execution
        """
        steps: List[WorkflowStep] = [
            CreateProjectStructureStep(self.generation_service),
            GenerateTypesEventsStep(self.generation_service),
        ]
        
        # Add machine generation steps
        for machine_name in machine_names:
            steps.append(
                GenerateMachineStep(self.generation_service, machine_name, ensemble_size=ensemble_size)
            )
        
        # Add spec and test
        steps.append(GenerateSpecStep(self.generation_service, spec_name, ensemble_size=ensemble_size))
        steps.append(GenerateTestStep(self.generation_service, test_name, ensemble_size=ensemble_size))
        
        # Save files
        steps.append(SaveGeneratedFilesStep())
        
        # Compile and fix
        steps.append(CompileProjectStep(self.compilation_service))
        steps.append(FixCompilationErrorsStep(self.fixer_service))
        
        return WorkflowDefinition(
            name="full_generation",
            description="Generate complete P project from design document",
            steps=steps,
            continue_on_failure=False
        )
    
    def create_add_machine_workflow(self, machine_name: str, ensemble_size: int = 3) -> WorkflowDefinition:
        """Create workflow to add a single machine to existing project."""
        return WorkflowDefinition(
            name=f"add_machine_{machine_name}",
            description=f"Add {machine_name} machine to project",
            steps=[
                GenerateMachineStep(self.generation_service, machine_name, ensemble_size=ensemble_size),
                SaveGeneratedFilesStep(),
                CompileProjectStep(self.compilation_service),
                FixCompilationErrorsStep(self.fixer_service),
            ],
            continue_on_failure=False
        )
    
    def create_add_spec_workflow(self, spec_name: str, ensemble_size: int = 3) -> WorkflowDefinition:
        """Create workflow to add a specification to existing project."""
        return WorkflowDefinition(
            name=f"add_spec_{spec_name}",
            description=f"Add {spec_name} specification to project",
            steps=[
                GenerateSpecStep(self.generation_service, spec_name, ensemble_size=ensemble_size),
                SaveGeneratedFilesStep(),
                CompileProjectStep(self.compilation_service),
                FixCompilationErrorsStep(self.fixer_service),
            ],
            continue_on_failure=False
        )
    
    def create_compile_and_fix_workflow(self) -> WorkflowDefinition:
        """Create workflow to compile and fix errors."""
        return WorkflowDefinition(
            name="compile_and_fix",
            description="Compile project and fix errors",
            steps=[
                CompileProjectStep(self.compilation_service),
                FixCompilationErrorsStep(self.fixer_service),
            ],
            continue_on_failure=False
        )
    
    def create_full_verification_workflow(
        self,
        schedules: int = 100,
        timeout: int = 60
    ) -> WorkflowDefinition:
        """Create workflow for full compilation, verification, and automatic bug fixing.
        
        Steps:
        1. Compile the project
        2. Fix any compilation errors (iteratively)
        3. Run PChecker to find bugs
        4. Automatically fix PChecker bugs using trace analysis
        
        The RunCheckerStep always propagates its output (even on failure) so that
        FixCheckerErrorsStep can read the trace files and apply AI-driven fixes.
        """
        return WorkflowDefinition(
            name="full_verification",
            description="Compile, fix errors, run PChecker, and automatically fix PChecker bugs",
            steps=[
                CompileProjectStep(self.compilation_service),
                FixCompilationErrorsStep(self.fixer_service),
                RunCheckerStep(self.compilation_service, schedules, timeout),
                FixCheckerErrorsStep(self.fixer_service),
            ],
            continue_on_failure=False
        )
    
    def create_quick_check_workflow(
        self,
        schedules: int = 100,
        timeout: int = 60
    ) -> WorkflowDefinition:
        """Create workflow for just running PChecker."""
        return WorkflowDefinition(
            name="quick_check",
            description="Run PChecker on project",
            steps=[
                RunCheckerStep(self.compilation_service, schedules, timeout),
            ],
            continue_on_failure=False
        )


def _to_pascal_case(name: str) -> str:
    """Convert a multi-word name to PascalCase for P machine naming.

    "Front Desk" -> "FrontDesk", "Lock Server" -> "LockServer",
    "CoffeeMaker" -> "CoffeeMaker" (already single word, preserved).
    """
    words = name.strip().split()
    if len(words) == 1:
        return words[0]
    return "".join(w[0].upper() + w[1:] for w in words if w)


# ── LLM-based extraction (primary) ──────────────────────────────────

_EXTRACT_PROMPT = """\
You are given a design document for a P language state machine system.

Your task: identify every component that should become a P `machine`.
For each component, return a PascalCase name suitable as a P machine
identifier (no spaces, no special characters).

Rules:
- Multi-word names become PascalCase: "Front Desk" -> "FrontDesk", "Lock Server" -> "LockServer"
- Only include components that are active participants (state machines). Do NOT include:
  - Abstract concepts (e.g., "Safety", "Specification")
  - Data types or events
  - Roles or states within a machine (e.g., "Follower", "Candidate" are roles of a Server, not separate machines)
- Return ONLY a JSON array of strings, nothing else.

Example output: ["Coordinator", "Participant", "Client", "Timer"]
"""


def _extract_names_with_llm(
    design_doc: str,
    llm_provider=None,
) -> Optional[List[str]]:
    """Use the LLM to extract machine names. Returns None on failure."""
    import json as _json
    try:
        from ..llm import LLMConfig, Message, MessageRole

        if llm_provider is None:
            from ..llm import get_default_provider
            llm_provider = get_default_provider()

        messages = [
            Message(role=MessageRole.USER, content=_EXTRACT_PROMPT),
            Message(role=MessageRole.USER, content=f"Design document:\n{design_doc}"),
        ]
        config = LLMConfig(max_tokens=256)
        response = llm_provider.complete(messages, config)
        text = response.content.strip()

        # Extract JSON array from the response (may be wrapped in markdown)
        arr_match = re.search(r'\[.*\]', text, re.DOTALL)
        if not arr_match:
            return None
        names = _json.loads(arr_match.group(0))
        if not isinstance(names, list):
            return None

        # Sanitise: PascalCase, no empty strings, no duplicates
        clean = []
        seen: set = set()
        for n in names:
            if not isinstance(n, str) or not n:
                continue
            pc = _to_pascal_case(n)
            if pc and pc not in seen and pc[0].isupper():
                clean.append(pc)
                seen.add(pc)
        return sorted(clean) if clean else None

    except Exception:
        return None


# ── Regex fallback (for offline / no-LLM environments) ──────────────

def _extract_section(text: str, section_name: str) -> Optional[str]:
    """Extract a section from a design doc using markdown headings.

    Matches ``# Section Name``, ``## Section Name``, etc. and returns
    everything until the next heading of equal or higher level.
    Underscores in *section_name* are treated as spaces so that
    ``"test_scenarios"`` matches ``## Test Scenarios``.
    """
    heading_label = section_name.replace("_", " ")
    md_match = re.search(
        rf'^(#{1,3})\s+{re.escape(heading_label)}\s*$',
        text, re.MULTILINE | re.IGNORECASE,
    )
    if md_match:
        level = len(md_match.group(1))
        rest = text[md_match.end():]
        next_heading = re.search(rf'^#{{{1},{level}}}\s', rest, re.MULTILINE)
        return rest[:next_heading.start()] if next_heading else rest

    return None


def _extract_title(text: str) -> Optional[str]:
    """Extract the document title from a markdown ``# Title`` heading."""
    m = re.search(r'^#\s+(.+?)\s*$', text, re.MULTILINE)
    return m.group(1).strip() if m else None


def _extract_names_regex(design_doc: str) -> List[str]:
    """Regex extraction from Components section as fallback (XML or markdown)."""
    names: set = set()

    comp_text = _extract_section(design_doc, "components")
    if comp_text:
        for m in re.finditer(
            r'^\s*(?:\d+\.|#{1,4})\s*(?:\d+\.\s*)?(.+?)\s*$',
            comp_text, re.MULTILINE,
        ):
            raw = m.group(1).strip().strip('*').strip()
            if raw and raw[0].isupper() and len(raw) < 40:
                names.add(_to_pascal_case(raw))

    if not names:
        for m in re.finditer(r'\bmachine\s+(\w+)\s*[{:]', design_doc):
            names.add(m.group(1))

    stop = {
        "machine", "state", "event", "type", "spec", "test", "main",
        "source", "target", "payload", "description", "effects",
        "source components", "test components", "role", "behavior",
    }
    return sorted(n for n in names if n.lower() not in stop and n[0].isupper())


# ── Public API ───────────────────────────────────────────────────────

def extract_machine_names_from_design_doc(
    design_doc: str,
    llm_provider=None,
) -> List[str]:
    """
    Extract P machine names from a design document.

    Uses the LLM for robust understanding of natural language component
    descriptions, with a regex fallback for offline environments.

    Multi-word names are automatically converted to PascalCase
    (e.g., "Front Desk" -> "FrontDesk").

    Args:
        design_doc: The design document content.
        llm_provider: Optional LLM provider instance.  If not given,
                      the function tries ``get_default_provider()``.

    Returns:
        Sorted list of unique PascalCase machine names.
    """
    llm_result = _extract_names_with_llm(design_doc, llm_provider=llm_provider)
    if llm_result:
        return llm_result

    return _extract_names_regex(design_doc)


def validate_design_doc(design_doc: str) -> Dict[str, Any]:
    """
    Validate a design document for completeness and consistency.

    Expects markdown-formatted design docs with headings:
    ``# Title``, ``## Components``, ``## Interactions``,
    ``## Specifications``, ``## Test Scenarios``.

    Returns:
        Dictionary with 'valid' (bool), 'warnings' (list), 'errors' (list),
        'components' (list), 'scenarios_count' (int)
    """
    result: Dict[str, Any] = {
        "valid": True,
        "warnings": [],
        "errors": [],
        "components": [],
        "scenarios_count": 0,
    }

    # Check required sections
    for section in ("title", "components", "interactions"):
        if section == "title":
            found = _extract_title(design_doc) is not None
        else:
            found = _extract_section(design_doc, section) is not None
        if not found:
            result["errors"].append(f"Missing required section: {section}")
            result["valid"] = False

    # Extract component names
    components = extract_machine_names_from_design_doc(design_doc)
    result["components"] = components
    if not components:
        result["errors"].append("No components/machines could be extracted from design doc")
        result["valid"] = False

    # Check for scenarios
    scenarios_text = _extract_section(design_doc, "test scenarios")
    if scenarios_text:
        scenario_lines = [
            l.strip() for l in scenarios_text.strip().split('\n')
            if l.strip() and re.match(r'\d+\.', l.strip())
        ]
        result["scenarios_count"] = len(scenario_lines)
        if not scenario_lines:
            result["warnings"].append("No numbered scenarios found in Test Scenarios section")
    else:
        result["warnings"].append("Missing 'Test Scenarios' section — tests may not be generated")

    # Check for specifications
    if _extract_section(design_doc, "specifications") is None:
        result["warnings"].append("Missing 'Specifications' — no safety/liveness specs will be generated")

    return result


def create_workflow_engine_from_config(
    config_path: str,
    generation_service: GenerationService,
    compilation_service: CompilationService,
    fixer_service: FixerService
) -> WorkflowEngine:
    """
    Create a WorkflowEngine with workflows loaded from YAML config.
    
    Args:
        config_path: Path to workflows.yaml
        generation_service: Service for code generation
        compilation_service: Service for compilation
        fixer_service: Service for error fixing
        
    Returns:
        Configured WorkflowEngine
    """
    emitter = EventEmitter()
    engine = WorkflowEngine(emitter)
    factory = WorkflowFactory(generation_service, compilation_service, fixer_service)
    
    # Load config
    with open(config_path, 'r') as f:
        config = yaml.safe_load(f)
    
    # Register standard workflows
    engine.register_workflow(
        factory.create_compile_and_fix_workflow()
    )
    
    checker_config = config.get('checker', {})
    engine.register_workflow(
        factory.create_full_verification_workflow(
            schedules=checker_config.get('default_schedules', 100),
            timeout=checker_config.get('default_timeout', 60)
        )
    )
    
    engine.register_workflow(
        factory.create_quick_check_workflow(
            schedules=checker_config.get('default_schedules', 100),
            timeout=checker_config.get('default_timeout', 60)
        )
    )
    
    return engine
