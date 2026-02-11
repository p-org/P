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
        test_name: str = "TestDriver"
    ) -> WorkflowDefinition:
        """
        Create a full project generation workflow.
        
        Args:
            machine_names: List of machine names to generate
            spec_name: Name for the specification file
            test_name: Name for the test file
            
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
                GenerateMachineStep(self.generation_service, machine_name)
            )
        
        # Add spec and test
        steps.append(GenerateSpecStep(self.generation_service, spec_name))
        steps.append(GenerateTestStep(self.generation_service, test_name))
        
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
    
    def create_add_machine_workflow(self, machine_name: str) -> WorkflowDefinition:
        """Create workflow to add a single machine to existing project."""
        return WorkflowDefinition(
            name=f"add_machine_{machine_name}",
            description=f"Add {machine_name} machine to project",
            steps=[
                GenerateMachineStep(self.generation_service, machine_name),
                SaveGeneratedFilesStep(),
                CompileProjectStep(self.compilation_service),
                FixCompilationErrorsStep(self.fixer_service),
            ],
            continue_on_failure=False
        )
    
    def create_add_spec_workflow(self, spec_name: str) -> WorkflowDefinition:
        """Create workflow to add a specification to existing project."""
        return WorkflowDefinition(
            name=f"add_spec_{spec_name}",
            description=f"Add {spec_name} specification to project",
            steps=[
                GenerateSpecStep(self.generation_service, spec_name),
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
        """Create workflow for full compilation and verification."""
        return WorkflowDefinition(
            name="full_verification",
            description="Compile, fix errors, and run PChecker",
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


def extract_machine_names_from_design_doc(design_doc: str) -> List[str]:
    """
    Extract machine names from a design document.
    
    Looks for patterns like:
    - <component>MachineName</component>
    - **MachineName** machine
    - The MachineName state machine
    - machine MachineName
    
    Args:
        design_doc: The design document content
        
    Returns:
        List of extracted machine names
    """
    machine_names = set()
    
    # Pattern 1: XML-style component tags
    xml_pattern = r'<component[^>]*>\s*(\w+)\s*</component>'
    for match in re.finditer(xml_pattern, design_doc, re.IGNORECASE):
        machine_names.add(match.group(1))
    
    # Pattern 2: Markdown bold with "machine" keyword
    md_pattern = r'\*\*(\w+)\*\*\s+(?:state\s+)?machine'
    for match in re.finditer(md_pattern, design_doc, re.IGNORECASE):
        machine_names.add(match.group(1))
    
    # Pattern 3: "The X machine" or "X state machine"
    prose_pattern = r'(?:the\s+)?(\w+)\s+(?:state\s+)?machine\b'
    for match in re.finditer(prose_pattern, design_doc, re.IGNORECASE):
        name = match.group(1)
        # Filter out common words
        if name.lower() not in ['a', 'the', 'this', 'each', 'every', 'state', 'new']:
            machine_names.add(name)
    
    # Pattern 4: P syntax "machine X"
    p_syntax_pattern = r'\bmachine\s+(\w+)\s*[{:]'
    for match in re.finditer(p_syntax_pattern, design_doc):
        machine_names.add(match.group(1))
    
    # Pattern 5: Components section with bullet points
    # - Client: ...
    # - Server: ...
    components_section = re.search(
        r'<components>(.*?)</components>',
        design_doc,
        re.DOTALL | re.IGNORECASE
    )
    if components_section:
        bullet_pattern = r'[-*]\s*(\w+)\s*:'
        for match in re.finditer(bullet_pattern, components_section.group(1)):
            machine_names.add(match.group(1))

        # Pattern 6: Numbered component lists
        # 1. Proposer
        # 2. Acceptor
        numbered_pattern = r'^\s*\d+\.\s*([A-Z]\w+)\b'
        for match in re.finditer(numbered_pattern, components_section.group(1), re.MULTILINE):
            machine_names.add(match.group(1))
    
    # Filter and clean
    filtered = []
    for name in machine_names:
        # Skip common non-machine words
        if name.lower() in ['machine', 'state', 'event', 'type', 'spec', 'test', 'main']:
            continue
        # Should start with uppercase (P convention)
        if name[0].isupper():
            filtered.append(name)
    
    return sorted(filtered)


def validate_design_doc(design_doc: str) -> Dict[str, Any]:
    """
    Validate a design document for completeness and consistency.

    Checks:
    - Has required sections (title, components, interactions, scenarios)
    - Scenarios reference components that exist
    - Interactions reference valid source/target components

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
    required_sections = {
        "title": r'<title>(.*?)</title>',
        "components": r'<components>(.*?)</components>',
        "interactions": r'<interactions>(.*?)</interactions>',
    }
    for section, pattern in required_sections.items():
        if not re.search(pattern, design_doc, re.DOTALL | re.IGNORECASE):
            result["errors"].append(f"Missing required section: <{section}>")
            result["valid"] = False

    # Extract component names
    components = extract_machine_names_from_design_doc(design_doc)
    result["components"] = components
    if not components:
        result["errors"].append("No components/machines could be extracted from design doc")
        result["valid"] = False

    # Check for scenarios
    scenarios_section = re.search(
        r'<possible_scenarios>(.*?)</possible_scenarios>',
        design_doc, re.DOTALL | re.IGNORECASE
    )
    if scenarios_section:
        scenario_lines = [
            l.strip() for l in scenarios_section.group(1).strip().split('\n')
            if l.strip() and re.match(r'\d+\.', l.strip())
        ]
        result["scenarios_count"] = len(scenario_lines)
        if not scenario_lines:
            result["warnings"].append("No numbered scenarios found in <possible_scenarios>")
    else:
        result["warnings"].append("Missing <possible_scenarios> section — tests may not be generated")

    # Check for global specifications
    specs_section = re.search(
        r'<global_specifications>(.*?)</global_specifications>',
        design_doc, re.DOTALL | re.IGNORECASE
    )
    if not specs_section:
        result["warnings"].append("Missing <global_specifications> — no safety/liveness specs will be generated")

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
