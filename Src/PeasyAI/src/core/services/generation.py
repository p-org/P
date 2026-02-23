"""
Generation Service

Handles P code generation from design documents.
This service is UI-agnostic and can be used by Streamlit, CLI, or MCP.

Supports RAG (Retrieval-Augmented Generation) for improved code quality
by providing relevant examples from the P program corpus.
"""

import os
import re
import logging
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path
from typing import Dict, Any, Optional, List
from dataclasses import dataclass, field

from .base import BaseService, ServiceResult, EventCallback, ResourceLoader
from ..llm import LLMProvider, LLMConfig, Message, MessageRole

# Try to import RAG service
try:
    from ..rag import get_rag_service, RAGService
    HAS_RAG = True
except ImportError:
    HAS_RAG = False

logger = logging.getLogger(__name__)

# Directory constants
PSRC = 'PSrc'
PSPEC = 'PSpec'
PTST = 'PTst'


@dataclass
class GenerationResult(ServiceResult):
    """Result of a generation operation"""
    filename: Optional[str] = None
    file_path: Optional[str] = None
    code: Optional[str] = None
    raw_response: Optional[str] = None


@dataclass
class ProjectGenerationResult(ServiceResult):
    """Result of a full project generation"""
    project_path: Optional[str] = None
    project_name: Optional[str] = None
    files_generated: List[str] = field(default_factory=list)
    compilation_success: bool = False
    compilation_output: Optional[str] = None


class GenerationService(BaseService):
    """
    Service for generating P code.
    
    Provides methods for:
    - Generating project structure
    - Generating types/events files
    - Generating machine implementations
    - Generating specs and tests
    - Running sanity checks
    
    Optionally uses RAG (Retrieval-Augmented Generation) to provide
    relevant examples from the P program corpus for improved generation.
    
    Generation methods automatically retry on extraction failure
    (up to MAX_GENERATION_RETRIES times) to mitigate LLM non-determinism.
    """

    MAX_GENERATION_RETRIES = 2
    
    def __init__(
        self,
        llm_provider: Optional[LLMProvider] = None,
        resource_loader: Optional[ResourceLoader] = None,
        callbacks: Optional[EventCallback] = None,
        use_rag: bool = True,
    ):
        super().__init__(llm_provider, resource_loader, callbacks)
        
        # Initialize RAG service if available
        self._rag: Optional['RAGService'] = None
        if use_rag and HAS_RAG:
            try:
                self._rag = get_rag_service()
                logger.info(f"RAG enabled with {self._rag.get_stats()['total_examples']} examples")
            except Exception as e:
                logger.warning(f"Failed to initialize RAG: {e}")
        # Cache static modular contexts/instructions to avoid repeated disk reads and
        # to support compact prompt assembly.
        self._static_context_cache: Dict[str, str] = {}
        # Soft caps to keep prompts concise and reduce LLM latency/cost.
        self._guide_char_limit = 3500
        self._context_file_char_limit = 9000
        self._design_doc_char_limit = 7000
        self._rag_context_char_limit = 7000
    
    _TIMER_TEMPLATE: Optional[str] = None

    @classmethod
    def _load_timer_template(cls) -> str:
        """Load the Common_Timer template on first use."""
        if cls._TIMER_TEMPLATE is None:
            # __file__ = src/core/services/generation.py → 4 parents to reach PeasyAI/
            timer_path = Path(__file__).resolve().parent.parent.parent.parent / "resources" / "rag_examples" / "Common_Timer" / "PSrc" / "Timer.p"
            try:
                cls._TIMER_TEMPLATE = timer_path.read_text(encoding="utf-8")
            except Exception:
                cls._TIMER_TEMPLATE = ""
        return cls._TIMER_TEMPLATE

    @staticmethod
    def _needs_timer(design_doc: str, context_files: Optional[Dict[str, str]] = None) -> bool:
        """Detect whether the protocol requires a Timer machine."""
        text = (design_doc or "").lower()
        if context_files:
            text += " " + " ".join(c.lower() for c in context_files.values())
        return any(kw in text for kw in [
            "timer", "timeout", "heartbeat", "periodic", "etimeout",
            "estarttimer", "ecanceltimer", "heartbeat interval",
        ])

    def _inject_timer_context(
        self,
        context_files: Optional[Dict[str, str]],
        design_doc: str,
    ) -> Dict[str, str]:
        """
        If the design doc uses timer/heartbeat patterns, inject the
        Common_Timer reference as a context file so the LLM uses the
        standard ``CreateTimer``/``StartTimer``/``CancelTimer`` API
        instead of reinventing the Timer machine.
        """
        ctx = dict(context_files) if context_files else {}
        if not self._needs_timer(design_doc, context_files):
            return ctx
        # Don't inject if there's already a Timer file
        if any("timer" in k.lower() for k in ctx):
            return ctx
        template = self._load_timer_template()
        if template:
            ctx["__Timer_Reference__.p"] = (
                "// REFERENCE: Standard Timer machine from the P tutorial.\n"
                "// Use CreateTimer(this), StartTimer(timer), CancelTimer(timer)\n"
                "// to interact with timers. Do NOT re-implement the Timer machine.\n"
                "// The Timer module is composed via: module MyModule = (union {...}, Timer);\n\n"
                + template
            )
        return ctx

    def _get_rag_context(
        self,
        context_type: str,
        description: str,
        design_doc: Optional[str] = None,
        context_files: Optional[Dict[str, str]] = None,
    ) -> str:
        """Get RAG context for generation, enriched by already-generated files."""
        if self._rag is None:
            return ""
        
        try:
            if context_type == "machine":
                context = self._rag.get_machine_context(
                    description, design_doc=design_doc, context_files=context_files
                )
            elif context_type == "spec":
                context = self._rag.get_spec_context(description)
            elif context_type == "test":
                context = self._rag.get_test_context(description)
            elif context_type == "types":
                context = self._rag.get_types_context(description)
            else:
                return ""
            
            return context.to_prompt_section()
        except Exception as e:
            logger.warning(f"Failed to get RAG context: {e}")
            return ""
    
    def create_project_structure(
        self,
        output_dir: str,
        project_name: str,
    ) -> GenerationResult:
        """
        Create P project folder structure.
        
        Args:
            output_dir: Directory to create project in
            project_name: Name of the project
            
        Returns:
            GenerationResult with project path
        """
        from datetime import datetime
        from utils.project_structure_utils import setup_project_structure
        
        self._status(f"Creating project structure for {project_name}...")
        
        try:
            timestamp = datetime.now().strftime('%Y_%m_%d_%H_%M_%S')
            project_name_with_timestamp = f"{project_name}_{timestamp}"
            project_root = os.path.join(output_dir, project_name_with_timestamp)
            
            os.makedirs(output_dir, exist_ok=True)
            setup_project_structure(project_root, project_name)
            
            self._status(f"Created project at {project_root}")
            
            return GenerationResult(
                success=True,
                file_path=project_root,
                filename=project_name_with_timestamp,
            )
        except Exception as e:
            logger.error(f"Error creating project structure: {e}")
            return GenerationResult(
                success=False,
                error=str(e),
            )
    
    def generate_types_events(
        self,
        design_doc: str,
        project_path: str,
        save_to_disk: bool = True,
    ) -> GenerationResult:
        """
        Generate Enums_Types_Events.p file.
        
        Args:
            design_doc: Design document content
            project_path: Path to the P project
            save_to_disk: If True, write to disk; if False, return code only for preview
            
        Returns:
            GenerationResult with generated code
        """
        self._status("Generating types, enums, and events...")
        
        try:
            # Build messages
            messages = self._build_types_events_messages(design_doc)
            
            # Get system prompt
            system_prompt = self.resources.load_context("about_p.txt")
            
            # Invoke LLM
            config = LLMConfig(max_tokens=4096)
            response = self.llm.complete(messages, config, system_prompt)
            
            # Extract code — retry on extraction failure
            filename, code = self._extract_p_code(response.content)

            if not filename or not code:
                for retry in range(self.MAX_GENERATION_RETRIES):
                    self._warning(f"Code extraction failed for types/events, retry {retry + 1}")
                    response = self.llm.complete(messages, config, system_prompt)
                    filename, code = self._extract_p_code(response.content)
                    if filename and code:
                        break

            if filename and code:
                file_path = os.path.join(project_path, PSRC, filename)
                
                # Only write if save_to_disk is True
                if save_to_disk:
                    os.makedirs(os.path.dirname(file_path), exist_ok=True)
                    with open(file_path, 'w') as f:
                        f.write(code)
                    self._status(f"Generated and saved {filename}")
                else:
                    self._status(f"Generated {filename} (preview only)")
                
                return GenerationResult(
                    success=True,
                    filename=filename,
                    file_path=file_path,
                    code=code,
                    raw_response=response.content,
                    token_usage=response.usage.to_dict(),
                )
            else:
                return GenerationResult(
                    success=False,
                    error="Could not extract P code from response after retries",
                    raw_response=response.content,
                    token_usage=response.usage.to_dict(),
                )
                
        except Exception as e:
            logger.error(f"Error generating types/events: {e}")
            return GenerationResult(
                success=False,
                error=str(e),
            )
    
    def generate_machine(
        self,
        machine_name: str,
        design_doc: str,
        project_path: str,
        context_files: Optional[Dict[str, str]] = None,
        two_stage: bool = True,
        save_to_disk: bool = True,
    ) -> GenerationResult:
        """
        Generate a P state machine.
        
        Args:
            machine_name: Name of the machine to generate
            design_doc: Design document content
            project_path: Path to the P project
            context_files: Additional context files (filename -> content)
            two_stage: Whether to use two-stage generation (structure first)
            save_to_disk: If True, write to disk; if False, return code only for preview
            
        Returns:
            GenerationResult with generated code
        """
        self._status(f"Generating machine: {machine_name}")
        
        try:
            system_prompt = self.resources.load_context("about_p.txt")
            
            if two_stage:
                # Stage 1: Generate structure
                self._status(f"  Stage 1: Generating structure for {machine_name}")
                structure_messages = self._build_machine_structure_messages(
                    machine_name, design_doc, context_files
                )
                
                config = LLMConfig(max_tokens=2048)
                structure_response = self.llm.complete(structure_messages, config, system_prompt)
                
                # Extract structure
                structure_match = re.search(
                    r'<structure>(.*?)</structure>',
                    structure_response.content,
                    re.DOTALL
                )
                
                if structure_match:
                    machine_structure = structure_match.group(1).strip()
                    
                    # Stage 2: Implement machine
                    self._status(f"  Stage 2: Implementing {machine_name}")
                    impl_messages = self._build_machine_impl_messages(
                        machine_name, design_doc, context_files, machine_structure
                    )
                    
                    config = LLMConfig(max_tokens=4096)
                    response = self.llm.complete(impl_messages, config, system_prompt)
                else:
                    # Fallback to single-stage
                    self._warning(f"Could not extract structure, falling back to single-stage")
                    impl_messages = self._build_machine_impl_messages(
                        machine_name, design_doc, context_files
                    )
                    config = LLMConfig(max_tokens=4096)
                    response = self.llm.complete(impl_messages, config, system_prompt)
            else:
                # Single-stage generation
                impl_messages = self._build_machine_impl_messages(
                    machine_name, design_doc, context_files
                )
                config = LLMConfig(max_tokens=4096)
                response = self.llm.complete(impl_messages, config, system_prompt)
            
            # Extract code — retry on extraction failure
            filename, code = self._extract_p_code(
                response.content, expected_name=machine_name
            )

            if not filename or not code:
                for retry in range(self.MAX_GENERATION_RETRIES):
                    self._warning(
                        f"Code extraction failed for {machine_name}, retry {retry + 1}/{self.MAX_GENERATION_RETRIES}"
                    )
                    impl_messages = self._build_machine_impl_messages(
                        machine_name, design_doc, context_files
                    )
                    config = LLMConfig(max_tokens=4096)
                    response = self.llm.complete(impl_messages, config, system_prompt)
                    filename, code = self._extract_p_code(
                        response.content, expected_name=machine_name
                    )
                    if filename and code:
                        break

            if filename and code:
                file_path = os.path.join(project_path, PSRC, filename)
                
                # Only write if save_to_disk is True
                if save_to_disk:
                    os.makedirs(os.path.dirname(file_path), exist_ok=True)
                    with open(file_path, 'w') as f:
                        f.write(code)
                    self._status(f"Generated and saved {filename}")
                else:
                    self._status(f"Generated {filename} (preview only)")
                
                return GenerationResult(
                    success=True,
                    filename=filename,
                    file_path=file_path,
                    code=code,
                    raw_response=response.content,
                    token_usage=response.usage.to_dict(),
                )
            else:
                return GenerationResult(
                    success=False,
                    error="Could not extract P code from response after retries",
                    raw_response=response.content,
                    token_usage=response.usage.to_dict(),
                )
                
        except Exception as e:
            logger.error(f"Error generating machine {machine_name}: {e}")
            return GenerationResult(
                success=False,
                error=str(e),
            )
    
    def generate_spec(
        self,
        spec_name: str,
        design_doc: str,
        project_path: str,
        context_files: Optional[Dict[str, str]] = None,
        save_to_disk: bool = True,
    ) -> GenerationResult:
        """
        Generate a P specification/monitor file.
        
        Args:
            spec_name: Name of the spec file
            design_doc: Design document content
            project_path: Path to the P project
            context_files: Additional context files
            save_to_disk: If True, write to disk; if False, return code only for preview
            
        Returns:
            GenerationResult with generated code
        """
        self._status(f"Generating specification: {spec_name}")
        
        try:
            messages = self._build_spec_messages(spec_name, design_doc, context_files)
            system_prompt = self.resources.load_context("about_p.txt")
            
            config = LLMConfig(max_tokens=4096)
            response = self.llm.complete(messages, config, system_prompt)
            
            filename, code = self._extract_p_code(
                response.content, expected_name=spec_name
            )

            if not filename or not code:
                for retry in range(self.MAX_GENERATION_RETRIES):
                    self._warning(f"Code extraction failed for spec {spec_name}, retry {retry + 1}")
                    response = self.llm.complete(messages, config, system_prompt)
                    filename, code = self._extract_p_code(
                        response.content, expected_name=spec_name
                    )
                    if filename and code:
                        break

            if filename and code:
                file_path = os.path.join(project_path, PSPEC, filename)
                
                # Only write if save_to_disk is True
                if save_to_disk:
                    os.makedirs(os.path.dirname(file_path), exist_ok=True)
                    with open(file_path, 'w') as f:
                        f.write(code)
                    self._status(f"Generated and saved {filename}")
                else:
                    self._status(f"Generated {filename} (preview only)")
                
                return GenerationResult(
                    success=True,
                    filename=filename,
                    file_path=file_path,
                    code=code,
                    raw_response=response.content,
                    token_usage=response.usage.to_dict(),
                )
            else:
                return GenerationResult(
                    success=False,
                    error="Could not extract P code from response after retries",
                    raw_response=response.content,
                )
                
        except Exception as e:
            logger.error(f"Error generating spec {spec_name}: {e}")
            return GenerationResult(
                success=False,
                error=str(e),
            )
    
    def generate_test(
        self,
        test_name: str,
        design_doc: str,
        project_path: str,
        context_files: Optional[Dict[str, str]] = None,
        save_to_disk: bool = True,
    ) -> GenerationResult:
        """
        Generate a P test file.
        
        Args:
            test_name: Name of the test file
            design_doc: Design document content
            project_path: Path to the P project
            context_files: Additional context files
            save_to_disk: If True, write to disk; if False, return code only for preview
            
        Returns:
            GenerationResult with generated code
        """
        self._status(f"Generating test: {test_name}")
        
        try:
            messages = self._build_test_messages(test_name, design_doc, context_files)
            system_prompt = self.resources.load_context("about_p.txt")
            
            config = LLMConfig(max_tokens=4096)
            response = self.llm.complete(messages, config, system_prompt)
            
            filename, code = self._extract_p_code(
                response.content, is_test_file=True, expected_name=test_name
            )

            if not filename or not code:
                for retry in range(self.MAX_GENERATION_RETRIES):
                    self._warning(f"Code extraction failed for test {test_name}, retry {retry + 1}")
                    response = self.llm.complete(messages, config, system_prompt)
                    filename, code = self._extract_p_code(
                        response.content, is_test_file=True, expected_name=test_name
                    )
                    if filename and code:
                        break

            if filename and code:
                file_path = os.path.join(project_path, PTST, filename)
                
                # Only write if save_to_disk is True
                if save_to_disk:
                    os.makedirs(os.path.dirname(file_path), exist_ok=True)
                    with open(file_path, 'w') as f:
                        f.write(code)
                    self._status(f"Generated and saved {filename}")
                else:
                    self._status(f"Generated {filename} (preview only)")
                
                return GenerationResult(
                    success=True,
                    filename=filename,
                    file_path=file_path,
                    code=code,
                    raw_response=response.content,
                    token_usage=response.usage.to_dict(),
                )
            else:
                return GenerationResult(
                    success=False,
                    error="Could not extract P code from response after retries",
                    raw_response=response.content,
                )
                
        except Exception as e:
            logger.error(f"Error generating test {test_name}: {e}")
            return GenerationResult(
                success=False,
                error=str(e),
            )
    
    def generate_machines_parallel(
        self,
        machine_names: List[str],
        design_doc: str,
        project_path: str,
        context_files: Optional[Dict[str, str]] = None,
        save_to_disk: bool = True,
        max_workers: int = 3,
    ) -> Dict[str, GenerationResult]:
        """
        Generate multiple machines in parallel.

        All machines receive the same context_files snapshot (types + any
        previously generated machines).  Results are returned keyed by
        machine name.

        Args:
            machine_names: List of machines to generate
            design_doc: Design document content
            project_path: Path to the P project
            context_files: Shared context (types file, etc.)
            save_to_disk: Whether to write files to disk
            max_workers: Maximum concurrent LLM calls

        Returns:
            Dictionary mapping machine name → GenerationResult
        """
        results: Dict[str, GenerationResult] = {}

        if len(machine_names) <= 1:
            # No benefit from parallelism
            for mn in machine_names:
                results[mn] = self.generate_machine(
                    mn, design_doc, project_path, context_files, save_to_disk=save_to_disk,
                )
            return results

        self._status(f"Generating {len(machine_names)} machines in parallel (max_workers={max_workers})")
        # Snapshot context so all threads see the same state
        ctx_snapshot = dict(context_files) if context_files else {}

        def _gen(name: str) -> tuple:
            return name, self.generate_machine(
                name, design_doc, project_path, ctx_snapshot, save_to_disk=save_to_disk,
            )

        with ThreadPoolExecutor(max_workers=max_workers) as pool:
            futures = {pool.submit(_gen, mn): mn for mn in machine_names}
            for future in as_completed(futures):
                mn = futures[future]
                try:
                    _, result = future.result()
                    results[mn] = result
                except Exception as exc:
                    logger.error(f"Parallel generation of {mn} failed: {exc}")
                    results[mn] = GenerationResult(success=False, error=str(exc))

        return results

    def generate_machine_ensemble(
        self,
        machine_name: str,
        design_doc: str,
        project_path: str,
        context_files: Optional[Dict[str, str]] = None,
        ensemble_size: int = 3,
        save_to_disk: bool = True,
    ) -> GenerationResult:
        """
        Generate a P state machine using ensemble: produce N candidates and
        pick the best one based on static quality scoring.

        This improves generation reliability by mitigating single-shot LLM
        non-determinism.  For example, some candidates will correctly add
        ``defer`` or ``ignore`` clauses for events that could arrive in
        unexpected states – a common source of PChecker failures.

        Args:
            machine_name: Name of the machine to generate
            design_doc: Design document content
            project_path: Path to the P project
            context_files: Additional context files
            ensemble_size: Number of candidates to generate (default 3)
            save_to_disk: Whether to write the best candidate to disk

        Returns:
            GenerationResult for the best candidate
        """
        first = self.generate_machine(
            machine_name, design_doc, project_path, context_files,
            save_to_disk=save_to_disk,
        )
        if ensemble_size <= 1:
            return first

        first_score = 0.0
        should_escalate = not first.success or not first.code
        if first.success and first.code:
            first_score = self._score_p_candidate(first.code, machine_name, context_files, file_type="machine")
            should_escalate = self._should_escalate_ensemble(first.code, first_score, "machine")

        if not should_escalate:
            self._status(f"Using first machine candidate for {machine_name} (score={first_score:.1f})")
            return first

        self._status(f"Escalating machine ensemble for {machine_name} (n={ensemble_size})")
        candidates: List[GenerationResult] = []
        if first.success and first.code:
            candidates.append(first)

        additional = max(0, ensemble_size - 1)

        def _gen(_idx: int) -> GenerationResult:
            return self.generate_machine(
                machine_name, design_doc, project_path, context_files,
                save_to_disk=False,
            )

        if additional > 0:
            with ThreadPoolExecutor(max_workers=min(additional, 4)) as pool:
                futures = [pool.submit(_gen, i) for i in range(additional)]
                for future in as_completed(futures):
                    try:
                        result = future.result()
                        if result.success and result.code:
                            candidates.append(result)
                    except Exception as e:
                        logger.warning(f"Ensemble candidate for {machine_name} failed: {e}")

        if not candidates:
            self._warning(f"All ensemble candidates failed for {machine_name}, returning first attempt")
            return first

        # Score with heuristic + optional compile-check bonus.
        scored = []
        for c in candidates:
            s = self._score_p_candidate(c.code, machine_name, context_files, file_type="machine")
            scored.append((c, s))

        # Try compile-check for top candidates when we have the compilation
        # service.  We only do this for machines (the most error-prone step)
        # and limit to the top-3 by heuristic score to bound latency.
        scored.sort(key=lambda t: t[1], reverse=True)
        compile_checked = self._compile_check_candidates(
            scored[:3], project_path, file_type="machine"
        )
        if compile_checked:
            scored = compile_checked

        best, best_score = max(scored, key=lambda t: t[1])
        self._status(f"Selected best of {len(candidates)} candidates for {machine_name} (score={best_score:.1f})")

        if save_to_disk and best.file_path:
            os.makedirs(os.path.dirname(best.file_path), exist_ok=True)
            with open(best.file_path, "w") as f:
                f.write(best.code)

        return best

    def generate_machines_ensemble(
        self,
        machine_names: List[str],
        design_doc: str,
        project_path: str,
        context_files: Optional[Dict[str, str]] = None,
        ensemble_size: int = 3,
        save_to_disk: bool = True,
    ) -> Dict[str, GenerationResult]:
        """
        Generate multiple machines sequentially, each with ensemble selection.

        Unlike ``generate_machines_parallel`` (which generates all machines
        concurrently with a shared context snapshot), this method generates
        machines one-by-one so that each subsequent machine can see the code
        of previously generated machines, improving cross-machine consistency.

        Args:
            machine_names: List of machine names to generate
            design_doc: Design document content
            project_path: Path to the P project
            context_files: Shared context (types file, etc.)
            ensemble_size: Number of candidates per machine
            save_to_disk: Whether to write files to disk

        Returns:
            Dictionary mapping machine name → GenerationResult
        """
        results: Dict[str, GenerationResult] = {}
        ctx = dict(context_files) if context_files else {}

        for mn in machine_names:
            result = self.generate_machine_ensemble(
                mn, design_doc, project_path, ctx,
                ensemble_size=ensemble_size, save_to_disk=save_to_disk,
            )
            results[mn] = result

            # Feed successful results as context for subsequent machines
            if result.success and result.code and result.filename:
                ctx[result.filename] = result.code

        return results

    def generate_spec_ensemble(
        self,
        spec_name: str,
        design_doc: str,
        project_path: str,
        context_files: Optional[Dict[str, str]] = None,
        ensemble_size: int = 3,
        save_to_disk: bool = True,
    ) -> GenerationResult:
        """Generate a P specification with ensemble selection."""
        first = self.generate_spec(
            spec_name, design_doc, project_path, context_files,
            save_to_disk=save_to_disk,
        )
        if ensemble_size <= 1:
            return first

        first_score = 0.0
        should_escalate = not first.success or not first.code
        if first.success and first.code:
            first_score = self._score_p_candidate(first.code, spec_name, context_files, file_type="spec")
            should_escalate = self._should_escalate_ensemble(first.code, first_score, "spec")

        if not should_escalate:
            self._status(f"Using first spec candidate for {spec_name} (score={first_score:.1f})")
            return first

        self._status(f"Escalating spec ensemble for {spec_name} (n={ensemble_size})")
        candidates: List[GenerationResult] = []
        if first.success and first.code:
            candidates.append(first)

        additional = max(0, ensemble_size - 1)

        def _gen(_idx: int) -> GenerationResult:
            return self.generate_spec(
                spec_name, design_doc, project_path, context_files,
                save_to_disk=False,
            )

        if additional > 0:
            with ThreadPoolExecutor(max_workers=min(additional, 4)) as pool:
                futures = [pool.submit(_gen, i) for i in range(additional)]
                for future in as_completed(futures):
                    try:
                        result = future.result()
                        if result.success and result.code:
                            candidates.append(result)
                    except Exception as e:
                        logger.warning(f"Ensemble candidate for spec {spec_name} failed: {e}")

        if not candidates:
            self._warning(f"All ensemble candidates failed for spec {spec_name}, returning first attempt")
            return first

        best = max(
            candidates,
            key=lambda c: self._score_p_candidate(c.code, spec_name, context_files, file_type="spec"),
        )
        best_score = self._score_p_candidate(best.code, spec_name, context_files, file_type="spec")
        self._status(f"Selected best of {len(candidates)} candidates for spec {spec_name} (score={best_score:.1f})")

        if save_to_disk and best.file_path:
            os.makedirs(os.path.dirname(best.file_path), exist_ok=True)
            with open(best.file_path, "w") as f:
                f.write(best.code)

        return best

    def generate_test_ensemble(
        self,
        test_name: str,
        design_doc: str,
        project_path: str,
        context_files: Optional[Dict[str, str]] = None,
        ensemble_size: int = 3,
        save_to_disk: bool = True,
    ) -> GenerationResult:
        """Generate a P test file with ensemble selection."""
        first = self.generate_test(
            test_name, design_doc, project_path, context_files,
            save_to_disk=save_to_disk,
        )
        if ensemble_size <= 1:
            return first

        first_score = 0.0
        should_escalate = not first.success or not first.code
        if first.success and first.code:
            first_score = self._score_p_candidate(first.code, test_name, context_files, file_type="test")
            should_escalate = self._should_escalate_ensemble(first.code, first_score, "test")

        if not should_escalate:
            self._status(f"Using first test candidate for {test_name} (score={first_score:.1f})")
            return first

        self._status(f"Escalating test ensemble for {test_name} (n={ensemble_size})")
        candidates: List[GenerationResult] = []
        if first.success and first.code:
            candidates.append(first)

        additional = max(0, ensemble_size - 1)

        def _gen(_idx: int) -> GenerationResult:
            return self.generate_test(
                test_name, design_doc, project_path, context_files,
                save_to_disk=False,
            )

        if additional > 0:
            with ThreadPoolExecutor(max_workers=min(additional, 4)) as pool:
                futures = [pool.submit(_gen, i) for i in range(additional)]
                for future in as_completed(futures):
                    try:
                        result = future.result()
                        if result.success and result.code:
                            candidates.append(result)
                    except Exception as e:
                        logger.warning(f"Ensemble candidate for test {test_name} failed: {e}")

        if not candidates:
            self._warning(f"All ensemble candidates failed for test {test_name}, returning first attempt")
            return first

        best = max(
            candidates,
            key=lambda c: self._score_p_candidate(c.code, test_name, context_files, file_type="test"),
        )
        best_score = self._score_p_candidate(best.code, test_name, context_files, file_type="test")
        self._status(f"Selected best of {len(candidates)} candidates for test {test_name} (score={best_score:.1f})")

        if save_to_disk and best.file_path:
            os.makedirs(os.path.dirname(best.file_path), exist_ok=True)
            with open(best.file_path, "w") as f:
                f.write(best.code)

        return best

    def save_p_file(
        self,
        file_path: str,
        code: str,
    ) -> GenerationResult:
        """
        Save P code to a file.
        
        Args:
            file_path: Absolute path where to save the file
            code: The P code content to save
            
        Returns:
            GenerationResult indicating success or failure
        """
        try:
            os.makedirs(os.path.dirname(file_path), exist_ok=True)
            
            with open(file_path, 'w') as f:
                f.write(code)
            
            filename = os.path.basename(file_path)
            self._status(f"Saved {filename}")
            
            return GenerationResult(
                success=True,
                filename=filename,
                file_path=file_path,
                code=code,
            )
        except Exception as e:
            logger.error(f"Error saving file {file_path}: {e}")
            return GenerationResult(
                success=False,
                error=str(e),
            )
    
    # =========================================================================
    # Private helper methods
    # =========================================================================
    
    def _score_p_candidate(
        self,
        code: str,
        name: str,
        context_files: Optional[Dict[str, str]],
        file_type: str = "machine",
    ) -> float:
        """
        Score a P code candidate for quality using static heuristics.

        Rewards correct structure and penalises common P syntax mistakes
        that would cause compilation failures.
        """
        if not code:
            return 0.0

        score = 0.0

        # ── Common scoring ────────────────────────────────────────────
        open_b = code.count("{")
        close_b = code.count("}")
        if open_b == close_b:
            score += 5
        else:
            score -= abs(open_b - close_b) * 2

        lines = len(code.strip().split("\n"))
        score += min(lines * 0.1, 10)

        # Event references from context (capped to avoid runaway bonus)
        if context_files:
            event_hits = 0
            for _fname, content in context_files.items():
                for ev in re.findall(r"\bevent\s+(\w+)", content):
                    if ev in code:
                        event_hits += 1
            score += min(event_hits * 2, 16)

        # ── Penalty: common P syntax errors ──────────────────────────
        # var x: int = 0;  (illegal inline init)
        score -= len(re.findall(r"\bvar\s+\w+\s*:\s*\w+\s*=", code)) * 5
        # Redeclared events/types in non-types files
        if file_type != "types":
            score -= len(re.findall(r"^\s*event\s+\w+", code, re.MULTILINE)) * 8
            score -= len(re.findall(r"^\s*type\s+\w+\s*=", code, re.MULTILINE)) * 8

        # ── Machine-specific scoring ──────────────────────────────────
        if file_type == "machine":
            if re.search(rf"\bmachine\s+{re.escape(name)}\b", code):
                score += 10
            if re.search(r"\bstart\s+state\b", code):
                score += 10
            state_count = len(re.findall(r"\bstate\s+\w+\s*\{", code))
            score += min(state_count * 3, 15)
            score += min(len(re.findall(r"\bentry\b", code)) * 2, 10)
            on_handlers = len(re.findall(r"\bon\s+\w+\s+(?:do|goto)\b", code))
            score += min(on_handlers * 2, 10)
            # defer/ignore — important but capped
            score += min(len(re.findall(r"\bdefer\b", code)) * 4, 16)
            score += min(len(re.findall(r"\bignore\b", code)) * 3, 12)
            # Bonus for send statements (shows the machine actually communicates)
            score += min(len(re.findall(r"\bsend\b", code)) * 1, 6)

        # ── Spec-specific scoring ─────────────────────────────────────
        elif file_type == "spec":
            if re.search(r"\bspec\s+\w+\s+observes\b", code):
                score += 15
            score += min(len(re.findall(r"\bassert\b", code)) * 5, 20)
            spec_count = len(re.findall(r"\bspec\s+\w+\b", code))
            score += min(spec_count * 5, 20)
            # Penalty: spec using forbidden keywords
            for kw in ["this", r"\bnew\s+\w+", r"\bsend\s+"]:
                if re.search(kw, code):
                    score -= 10

        # ── Test-specific scoring ─────────────────────────────────────
        elif file_type == "test":
            test_decl_count = len(re.findall(r"\btest\s+\w+\s*\[", code))
            score += min(test_decl_count * 10, 30)
            if re.search(r"\bassert\s+\w+\s+in\b", code):
                score += 10
            score += min(len(re.findall(r"\bnew\s+\w+", code)) * 2, 10)
            score += min(len(re.findall(r"\bmachine\s+Scenario\w*", code)) * 5, 15)
            # Bonus for proper sequence building: += (idx, value)
            score += min(len(re.findall(r"\+=\s*\(", code)) * 2, 8)

        return score

    def _should_escalate_ensemble(self, code: str, score: float, file_type: str) -> bool:
        """Return True when candidate quality suggests running additional ensemble attempts."""
        if not code:
            return True

        if file_type == "machine":
            has_machine_decl = bool(re.search(r"\bmachine\s+\w+\b", code))
            has_start_state = bool(re.search(r"\bstart\s+state\b", code))
            return score < 42 or not has_machine_decl or not has_start_state

        if file_type == "spec":
            has_spec_observes = bool(re.search(r"\bspec\s+\w+\s+observes\b", code))
            has_assert = bool(re.search(r"\bassert\b", code))
            return score < 28 or not has_spec_observes or not has_assert

        if file_type == "test":
            has_test_decl = bool(re.search(r"\btest\s+\w+\s*\[", code))
            has_new = bool(re.search(r"\bnew\s+", code))
            return score < 24 or not has_test_decl or not has_new

        return False

    def _compile_check_candidates(
        self,
        scored_candidates: List[tuple],
        project_path: str,
        file_type: str = "machine",
    ) -> Optional[List[tuple]]:
        """
        Attempt a compile-check on each candidate to add a strong signal.

        Temporarily writes each candidate's code to its target file,
        runs the P compiler, and adds a +50 bonus for candidates that
        compile successfully. Restores the original file after each
        check.

        Returns the re-scored list, or None if the compilation service
        is unavailable.
        """
        try:
            from ..compilation import CompilationService as _CS
        except ImportError:
            return None

        if not scored_candidates:
            return None

        # We need an existing file path to overwrite temporarily
        first_result = scored_candidates[0][0]
        target_path = first_result.file_path
        if not target_path or not os.path.exists(os.path.dirname(target_path)):
            return None

        # Save the current file content (if any) for restoration
        original_content = None
        if os.path.exists(target_path):
            try:
                with open(target_path, "r") as f:
                    original_content = f.read()
            except Exception:
                pass

        COMPILE_BONUS = 50
        re_scored = []

        for candidate, heuristic_score in scored_candidates:
            if not candidate.code:
                re_scored.append((candidate, heuristic_score))
                continue

            try:
                # Write candidate to disk
                os.makedirs(os.path.dirname(target_path), exist_ok=True)
                with open(target_path, "w") as f:
                    f.write(candidate.code)

                # Try compiling
                from ..compilation import ensure_environment
                env = ensure_environment()
                if env.is_valid and env.p_compiler_path:
                    import subprocess
                    result = subprocess.run(
                        [env.p_compiler_path, "compile"],
                        cwd=project_path,
                        capture_output=True,
                        text=True,
                        timeout=30,
                    )
                    if result.returncode == 0:
                        logger.info(f"  Candidate compiles successfully (+{COMPILE_BONUS} bonus)")
                        re_scored.append((candidate, heuristic_score + COMPILE_BONUS))
                    else:
                        re_scored.append((candidate, heuristic_score))
                else:
                    re_scored.append((candidate, heuristic_score))
            except Exception as e:
                logger.debug(f"  Compile check failed for candidate: {e}")
                re_scored.append((candidate, heuristic_score))

        # Restore original file
        try:
            if original_content is not None:
                with open(target_path, "w") as f:
                    f.write(original_content)
            elif os.path.exists(target_path):
                os.remove(target_path)
        except Exception:
            pass

        return re_scored

    def _build_types_events_messages(self, design_doc: str) -> List[Message]:
        """Build messages for types/events generation"""
        messages = []
        
        # Add P basics
        p_basics = self._load_static_modular_context("p_basics.txt")
        messages.append(Message(
            role=MessageRole.USER,
            content=f"Reference P Language Guide:\n{self._compact_text(p_basics, self._guide_char_limit)}"
        ))
        
        # Add specific guides
        types_guide = self._load_static_modular_context("p_types_guide.txt")
        events_guide = self._load_static_modular_context("p_events_guide.txt")
        enums_guide = self._load_static_modular_context("p_enums_guide.txt")
        
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<types_guide>\n{self._compact_text(types_guide, self._guide_char_limit)}\n</types_guide>"
        ))
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<events_guide>\n{self._compact_text(events_guide, self._guide_char_limit)}\n</events_guide>"
        ))
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<enums_guide>\n{self._compact_text(enums_guide, self._guide_char_limit)}\n</enums_guide>"
        ))
        
        # Add RAG context (similar type/event examples from corpus)
        rag_context = self._get_rag_context("types", "types and events", design_doc)
        if rag_context:
            messages.append(Message(
                role=MessageRole.USER,
                content=f"<similar_examples>\n{self._compact_text(rag_context, self._rag_context_char_limit)}\n</similar_examples>"
            ))
        
        # Add design doc
        messages.append(Message(
            role=MessageRole.USER,
            content=f"Design Document:\n{self._compact_design_doc(design_doc)}"
        ))
        
        # Add instruction
        instruction = self._load_static_instruction("generate_enums_types_events.txt")
        messages.append(Message(
            role=MessageRole.USER,
            content=instruction
        ))
        
        return messages
    
    def _build_machine_structure_messages(
        self,
        machine_name: str,
        design_doc: str,
        context_files: Optional[Dict[str, str]],
    ) -> List[Message]:
        """Build messages for machine structure generation"""
        # Auto-inject Timer template if the protocol uses timers
        context_files = self._inject_timer_context(context_files, design_doc)

        messages = []
        
        # Add guides
        p_basics = self._load_static_modular_context("p_basics.txt")
        machines_guide = self._load_static_modular_context("p_machines_guide.txt")
        
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<p_basics>\n{self._compact_text(p_basics, self._guide_char_limit)}\n</p_basics>"
        ))
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<machines_guide>\n{self._compact_text(machines_guide, self._guide_char_limit)}\n</machines_guide>"
        ))
        
        # Add RAG context enriched with already-generated files
        rag_context = self._get_rag_context(
            "machine", machine_name, design_doc, context_files=context_files
        )
        if rag_context:
            messages.append(Message(
                role=MessageRole.USER,
                content=f"<similar_examples>\n{self._compact_text(rag_context, self._rag_context_char_limit)}\n</similar_examples>"
            ))
        
        # Add context files
        if context_files:
            messages.extend(self._compact_context_messages(context_files))
        
        # Add design doc
        messages.append(Message(
            role=MessageRole.USER,
            content=f"Design Document:\n{self._compact_design_doc(design_doc)}"
        ))
        
        # Add instruction
        instruction = self._load_static_instruction("generate_machine_structure.txt")
        messages.append(Message(
            role=MessageRole.USER,
            content=instruction.format(machineName=machine_name)
        ))
        
        return messages
    
    def _build_machine_impl_messages(
        self,
        machine_name: str,
        design_doc: str,
        context_files: Optional[Dict[str, str]],
        structure: Optional[str] = None,
    ) -> List[Message]:
        """Build messages for machine implementation"""
        context_files = self._inject_timer_context(context_files, design_doc)

        messages = []
        
        # Add guides
        p_basics = self._load_static_modular_context("p_basics.txt")
        machines_guide = self._load_static_modular_context("p_machines_guide.txt")
        statements_guide = self._load_static_modular_context("p_statements_guide.txt")
        
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<p_basics>\n{self._compact_text(p_basics, self._guide_char_limit)}\n</p_basics>"
        ))
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<machines_guide>\n{self._compact_text(machines_guide, self._guide_char_limit)}\n</machines_guide>"
        ))
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<statements_guide>\n{self._compact_text(statements_guide, self._guide_char_limit)}\n</statements_guide>"
        ))
        
        # Add context files
        if context_files:
            messages.extend(self._compact_context_messages(context_files))
        
        # Add design doc
        messages.append(Message(
            role=MessageRole.USER,
            content=f"Design Document:\n{self._compact_design_doc(design_doc)}"
        ))
        
        # Add instruction with optional structure
        instruction = self._load_static_instruction("generate_machine.txt")
        content = instruction.format(machineName=machine_name)
        
        if structure:
            content += f"\n\nHere is the starting structure:\n\n{structure}"
        
        messages.append(Message(
            role=MessageRole.USER,
            content=content
        ))
        
        return messages
    
    def _build_spec_messages(
        self,
        spec_name: str,
        design_doc: str,
        context_files: Optional[Dict[str, str]],
    ) -> List[Message]:
        """Build messages for spec generation"""
        messages = []
        
        # Add guides
        p_basics = self._load_static_modular_context("p_basics.txt")
        spec_guide = self._load_static_modular_context("p_spec_monitors_guide.txt")
        
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<p_basics>\n{self._compact_text(p_basics, self._guide_char_limit)}\n</p_basics>"
        ))
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<spec_guide>\n{self._compact_text(spec_guide, self._guide_char_limit)}\n</spec_guide>"
        ))
        
        # Add RAG context (similar spec examples from corpus)
        rag_context = self._get_rag_context("spec", spec_name, design_doc)
        if rag_context:
            messages.append(Message(
                role=MessageRole.USER,
                content=f"<similar_examples>\n{self._compact_text(rag_context, self._rag_context_char_limit)}\n</similar_examples>"
            ))
        
        # Add context files
        if context_files:
            messages.extend(self._compact_context_messages(context_files))
        
        # Add design doc
        messages.append(Message(
            role=MessageRole.USER,
            content=f"Design Document:\n{self._compact_design_doc(design_doc)}"
        ))
        
        # Add instruction
        instruction = self._load_static_instruction("generate_spec_files.txt")
        messages.append(Message(
            role=MessageRole.USER,
            content=instruction.format(filename=spec_name)
        ))
        
        return messages
    
    def _build_test_messages(
        self,
        test_name: str,
        design_doc: str,
        context_files: Optional[Dict[str, str]],
    ) -> List[Message]:
        """Build messages for test generation"""
        messages = []
        
        # Add guides
        p_basics = self._load_static_modular_context("p_basics.txt")
        test_guide = self._load_static_modular_context("p_test_cases_guide.txt")
        
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<p_basics>\n{self._compact_text(p_basics, self._guide_char_limit)}\n</p_basics>"
        ))
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<test_guide>\n{self._compact_text(test_guide, self._guide_char_limit)}\n</test_guide>"
        ))
        
        # Add RAG context (similar test driver examples from corpus)
        rag_context = self._get_rag_context("test", test_name, design_doc)
        if rag_context:
            messages.append(Message(
                role=MessageRole.USER,
                content=f"<similar_examples>\n{self._compact_text(rag_context, self._rag_context_char_limit)}\n</similar_examples>"
            ))
        
        # Add context files
        if context_files:
            messages.extend(self._compact_context_messages(context_files))
        
        # Extract and inject machine wiring information so the LLM
        # knows the exact constructor signature for each machine.
        wiring_info = self._extract_machine_wiring_info(context_files)
        if wiring_info:
            messages.append(Message(
                role=MessageRole.USER,
                content=(
                    "<machine_wiring_reference>\n"
                    "Below is the EXACT constructor/initialization signature for each machine. "
                    "You MUST match these when creating machines with `new` or sending config events.\n\n"
                    f"{wiring_info}\n"
                    "</machine_wiring_reference>"
                ),
            ))

        spec_names = self._extract_spec_monitor_names(context_files)
        if spec_names:
            messages.append(Message(
                role=MessageRole.USER,
                content=(
                    "<spec_monitor_reference>\n"
                    "The following spec monitors are defined in the PSpec files. "
                    "Use EXACTLY these names in the `assert` clauses of test declarations.\n"
                    + "\n".join(f"- {name}" for name in spec_names) + "\n"
                    "</spec_monitor_reference>"
                ),
            ))
        
        # Add design doc
        messages.append(Message(
            role=MessageRole.USER,
            content=f"Design Document:\n{self._compact_design_doc(design_doc)}"
        ))
        
        # Add instruction
        instruction = self._load_static_instruction("generate_test_files.txt")
        messages.append(Message(
            role=MessageRole.USER,
            content=instruction.format(filename=test_name)
        ))
        
        return messages

    def _load_static_modular_context(self, filename: str) -> str:
        key = f"modular:{filename}"
        if key not in self._static_context_cache:
            self._static_context_cache[key] = self.resources.load_modular_context(filename)
        return self._static_context_cache[key]

    def _load_static_instruction(self, filename: str) -> str:
        key = f"instruction:{filename}"
        if key not in self._static_context_cache:
            self._static_context_cache[key] = self.resources.load_instruction(filename)
        return self._static_context_cache[key]

    def _compact_text(self, text: str, limit: int) -> str:
        if not text:
            return ""
        if len(text) <= limit:
            return text
        # Keep head + tail to preserve overview and concrete syntax examples.
        head = int(limit * 0.75)
        tail = limit - head
        return text[:head] + "\n... (truncated for prompt efficiency) ...\n" + text[-tail:]

    def _compact_design_doc(self, design_doc: str) -> str:
        return self._compact_text(design_doc, self._design_doc_char_limit)

    def _compact_context_messages(self, context_files: Dict[str, str]) -> List[Message]:
        messages: List[Message] = []
        # Prioritize critical files for correctness first.
        priority = []
        rest = []
        for filename, content in context_files.items():
            lname = filename.lower()
            if "enums" in lname or "types" in lname or "event" in lname:
                priority.append((filename, content))
            elif "safety" in lname or "spec" in lname:
                priority.append((filename, content))
            else:
                rest.append((filename, content))
        ordered = priority + rest

        budget = self._context_file_char_limit
        for filename, content in ordered:
            if budget <= 0:
                break
            chunk = self._compact_text(content, min(2500, budget))
            budget -= len(chunk)
            messages.append(
                Message(
                    role=MessageRole.USER,
                    content=f"<{filename}>\n{chunk}\n</{filename}>",
                )
            )
        return messages
    
    @staticmethod
    def _extract_machine_wiring_info(
        context_files: Optional[Dict[str, str]],
    ) -> str:
        """
        Extract machine initialization signatures from generated machine code.

        Scans context files for patterns like:
          machine Foo { ... start state Init { entry InitEntry; ... } ... fun InitEntry(cfg: ...) { ... } }
        and also config-event patterns like:
          start state WaitForConfig { on eMyConfig goto Ready with ConfigureHandler; }

        Returns a human-readable summary the LLM can use to wire machines
        correctly in the test driver.
        """
        if not context_files:
            return ""

        lines_out: list = []

        for filename, code in context_files.items():
            if not filename.endswith(".p"):
                continue
            # Skip types/events/spec files — we only want machines
            if "Enums" in filename or "Types" in filename or "Safety" in filename:
                continue

            # Find machine name
            machine_match = re.search(r"\bmachine\s+(\w+)\s*\{", code)
            if not machine_match:
                continue
            machine_name = machine_match.group(1)

            # Find start state (brace-balanced extraction)
            from ..compilation.p_code_utils import extract_start_state
            start_state_name, start_body = extract_start_state(code)
            if not start_state_name or start_body is None:
                continue

            # Pattern A: entry function with parameter (constructor payload)
            entry_match = re.search(r"\bentry\s+(\w+)\s*;", start_body)
            if entry_match:
                entry_fn_name = entry_match.group(1)
                # Find the function definition and its parameter
                fn_pattern = rf"\bfun\s+{re.escape(entry_fn_name)}\s*\(([^)]*)\)"
                fn_match = re.search(fn_pattern, code)
                if fn_match:
                    param_text = fn_match.group(1).strip()
                    if param_text:
                        lines_out.append(
                            f"- {machine_name}: created via `new {machine_name}({param_text.split(':',1)[-1].strip()})` "
                            f"  (entry function: `fun {entry_fn_name}({param_text})`)"
                        )
                    else:
                        lines_out.append(
                            f"- {machine_name}: created via `new {machine_name}()` — no constructor config needed"
                        )
                    continue

            # Pattern A alt: inline entry block (entry { ... }) — no config
            if re.search(r"\bentry\s*\{", start_body):
                lines_out.append(
                    f"- {machine_name}: created via `new {machine_name}()` — inline entry, no constructor config"
                )
                continue

            # Pattern B: config event in start state
            config_event_match = re.search(
                r"\bon\s+(\w+)\s+goto\s+\w+(?:\s+with\s+(\w+))?\s*;",
                start_body,
            )
            if config_event_match:
                event_name = config_event_match.group(1)
                handler_name = config_event_match.group(2)
                # Find handler parameter to get the payload type
                payload_info = ""
                if handler_name:
                    handler_fn = re.search(
                        rf"\bfun\s+{re.escape(handler_name)}\s*\(([^)]*)\)",
                        code,
                    )
                    if handler_fn and handler_fn.group(1).strip():
                        payload_info = f" with payload `{handler_fn.group(1).strip()}`"
                lines_out.append(
                    f"- {machine_name}: created via `new {machine_name}()`, then send `{event_name}`{payload_info}"
                )
                continue

            # Fallback — no config detected
            lines_out.append(
                f"- {machine_name}: created via `new {machine_name}()` — no config detected"
            )

        return "\n".join(lines_out)

    @staticmethod
    def _extract_spec_monitor_names(
        context_files: Optional[Dict[str, str]],
    ) -> List[str]:
        """
        Extract spec monitor names from context files (PSpec/*.p).

        Returns a list of spec machine names (e.g. ["Safety", "OnlyOneValueChosen"])
        that the test driver should reference in ``assert`` clauses.
        """
        if not context_files:
            return []

        names: List[str] = []
        for filename, code in context_files.items():
            if not filename.endswith(".p"):
                continue
            for m in re.finditer(r"\bspec\s+(\w+)\s+observes\b", code):
                names.append(m.group(1))
        return names

    def _extract_p_code(
        self,
        response: str,
        is_test_file: bool = False,
        expected_name: Optional[str] = None,
    ) -> tuple:
        """
        Extract P code from LLM response.
        
        Tries multiple extraction strategies in order:
        1. XML-style <filename.p>...</filename.p> tags
        2. Markdown code block with filename comment  (```p // filename.p ...)
        3. Markdown code block (```p ... ```) using first `machine` name
        
        Also post-processes the code to fix common issues.
        
        Args:
            response: Raw LLM response text
            is_test_file: If True, this is a PTst file; the post-processor
                          will ensure test declarations exist.
            expected_name: If provided, used as the fallback filename when
                           the LLM response doesn't include one (e.g. the
                           machine_name parameter from generate_machine).
        
        Returns:
            Tuple of (filename, code) or (None, None) if not found
        """
        from ..compilation.p_code_utils import extract_p_code_from_response

        filename, code = extract_p_code_from_response(
            response, expected_filename=expected_name
        )

        if not filename or not code:
            return None, None

        # Post-process the code to fix common issues
        try:
            from ..compilation.p_post_processor import PCodePostProcessor
            processor = PCodePostProcessor()
            result = processor.process(code, filename, is_test_file=is_test_file)
            code = result.code
            if result.fixes_applied:
                logger.info(f"Post-processing applied {len(result.fixes_applied)} fix(es) to {filename}")
                for fix in result.fixes_applied:
                    logger.debug(f"  - {fix}")
        except ImportError:
            logger.debug("PCodePostProcessor not available, skipping post-processing")
        except Exception as e:
            logger.warning(f"Post-processing failed: {e}")

        return filename, code
