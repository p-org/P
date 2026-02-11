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
    
    def _get_rag_context(self, context_type: str, description: str, design_doc: Optional[str] = None) -> str:
        """Get RAG context for generation."""
        if self._rag is None:
            return ""
        
        try:
            if context_type == "machine":
                context = self._rag.get_machine_context(description, design_doc=design_doc)
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
            filename, code = self._extract_p_code(response.content)

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
            
            filename, code = self._extract_p_code(response.content)

            if not filename or not code:
                for retry in range(self.MAX_GENERATION_RETRIES):
                    self._warning(f"Code extraction failed for spec {spec_name}, retry {retry + 1}")
                    response = self.llm.complete(messages, config, system_prompt)
                    filename, code = self._extract_p_code(response.content)
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
            
            filename, code = self._extract_p_code(response.content, is_test_file=True)

            if not filename or not code:
                for retry in range(self.MAX_GENERATION_RETRIES):
                    self._warning(f"Code extraction failed for test {test_name}, retry {retry + 1}")
                    response = self.llm.complete(messages, config, system_prompt)
                    filename, code = self._extract_p_code(response.content, is_test_file=True)
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
        if ensemble_size <= 1:
            return self.generate_machine(
                machine_name, design_doc, project_path, context_files,
                save_to_disk=save_to_disk,
            )

        self._status(f"Generating {machine_name} with ensemble (n={ensemble_size})")
        candidates: List[GenerationResult] = []

        def _gen(_idx: int) -> GenerationResult:
            return self.generate_machine(
                machine_name, design_doc, project_path, context_files,
                save_to_disk=False,
            )

        with ThreadPoolExecutor(max_workers=min(ensemble_size, 4)) as pool:
            futures = [pool.submit(_gen, i) for i in range(ensemble_size)]
            for future in as_completed(futures):
                try:
                    result = future.result()
                    if result.success and result.code:
                        candidates.append(result)
                except Exception as e:
                    logger.warning(f"Ensemble candidate for {machine_name} failed: {e}")

        if not candidates:
            self._warning(f"All ensemble candidates failed for {machine_name}, falling back to single generation")
            return self.generate_machine(
                machine_name, design_doc, project_path, context_files,
                save_to_disk=save_to_disk,
            )

        # Score and pick the best
        best = max(
            candidates,
            key=lambda c: self._score_p_candidate(c.code, machine_name, context_files, file_type="machine"),
        )
        self._status(
            f"Selected best of {len(candidates)} candidates for {machine_name} "
            f"(score={self._score_p_candidate(best.code, machine_name, context_files, file_type='machine'):.1f})"
        )

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
        if ensemble_size <= 1:
            return self.generate_spec(
                spec_name, design_doc, project_path, context_files,
                save_to_disk=save_to_disk,
            )

        self._status(f"Generating spec {spec_name} with ensemble (n={ensemble_size})")
        candidates: List[GenerationResult] = []

        def _gen(_idx: int) -> GenerationResult:
            return self.generate_spec(
                spec_name, design_doc, project_path, context_files,
                save_to_disk=False,
            )

        with ThreadPoolExecutor(max_workers=min(ensemble_size, 4)) as pool:
            futures = [pool.submit(_gen, i) for i in range(ensemble_size)]
            for future in as_completed(futures):
                try:
                    result = future.result()
                    if result.success and result.code:
                        candidates.append(result)
                except Exception as e:
                    logger.warning(f"Ensemble candidate for spec {spec_name} failed: {e}")

        if not candidates:
            self._warning(f"All ensemble candidates failed for spec {spec_name}, falling back")
            return self.generate_spec(
                spec_name, design_doc, project_path, context_files,
                save_to_disk=save_to_disk,
            )

        best = max(
            candidates,
            key=lambda c: self._score_p_candidate(c.code, spec_name, context_files, file_type="spec"),
        )
        self._status(f"Selected best of {len(candidates)} candidates for spec {spec_name}")

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
        if ensemble_size <= 1:
            return self.generate_test(
                test_name, design_doc, project_path, context_files,
                save_to_disk=save_to_disk,
            )

        self._status(f"Generating test {test_name} with ensemble (n={ensemble_size})")
        candidates: List[GenerationResult] = []

        def _gen(_idx: int) -> GenerationResult:
            return self.generate_test(
                test_name, design_doc, project_path, context_files,
                save_to_disk=False,
            )

        with ThreadPoolExecutor(max_workers=min(ensemble_size, 4)) as pool:
            futures = [pool.submit(_gen, i) for i in range(ensemble_size)]
            for future in as_completed(futures):
                try:
                    result = future.result()
                    if result.success and result.code:
                        candidates.append(result)
                except Exception as e:
                    logger.warning(f"Ensemble candidate for test {test_name} failed: {e}")

        if not candidates:
            self._warning(f"All ensemble candidates failed for test {test_name}, falling back")
            return self.generate_test(
                test_name, design_doc, project_path, context_files,
                save_to_disk=save_to_disk,
            )

        best = max(
            candidates,
            key=lambda c: self._score_p_candidate(c.code, test_name, context_files, file_type="test"),
        )
        self._status(f"Selected best of {len(candidates)} candidates for test {test_name}")

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

        Higher scores indicate higher-quality candidates.  The scoring
        rewards:
        - Correct P constructs (machine, start state, spec, test)
        - Use of ``defer`` / ``ignore`` (reduces PChecker unhandled-event bugs)
        - Event coverage (references to events from the types file)
        - Structural completeness (states, handlers, assertions)
        - Balanced braces (basic syntax sanity)
        """
        if not code:
            return 0.0

        score = 0.0

        # ── Common scoring (all file types) ────────────────────────────
        # Balanced braces
        open_b = code.count("{")
        close_b = code.count("}")
        if open_b == close_b:
            score += 5
        else:
            score -= abs(open_b - close_b) * 2

        # Code completeness (line count, diminishing returns)
        lines = len(code.strip().split("\n"))
        score += min(lines * 0.1, 10)

        # Event references from context
        if context_files:
            for _fname, content in context_files.items():
                for ev in re.findall(r"\bevent\s+(\w+)", content):
                    if ev in code:
                        score += 2

        # ── Machine-specific scoring ───────────────────────────────────
        if file_type == "machine":
            if re.search(rf"\bmachine\s+{re.escape(name)}\b", code):
                score += 10
            if re.search(r"\bstart\s+state\b", code):
                score += 10
            # States
            state_count = len(re.findall(r"\bstate\s+\w+\s*\{", code))
            score += min(state_count * 3, 15)
            # Entry handlers
            score += min(len(re.findall(r"\bentry\b", code)) * 2, 10)
            # on-event handlers
            score += min(len(re.findall(r"\bon\s+\w+\s+(?:do|goto)\b", code)) * 2, 10)
            # defer — VERY important for avoiding unhandled-event bugs
            score += len(re.findall(r"\bdefer\b", code)) * 5
            # ignore
            score += len(re.findall(r"\bignore\b", code)) * 3

        # ── Spec-specific scoring ──────────────────────────────────────
        elif file_type == "spec":
            if re.search(r"\bspec\s+\w+\s+observes\b", code):
                score += 15
            score += len(re.findall(r"\bassert\b", code)) * 5
            spec_count = len(re.findall(r"\bspec\s+\w+\b", code))
            score += min(spec_count * 5, 20)

        # ── Test-specific scoring ──────────────────────────────────────
        elif file_type == "test":
            test_decl_count = len(re.findall(r"\btest\s+\w+\s*\[", code))
            score += test_decl_count * 10
            if re.search(r"\bassert\s+\w+\s+in\b", code):
                score += 10
            score += len(re.findall(r"\bnew\s+\w+", code)) * 2
            # Scenario machines
            score += len(re.findall(r"\bmachine\s+Scenario\w*", code)) * 5

        return score

    def _build_types_events_messages(self, design_doc: str) -> List[Message]:
        """Build messages for types/events generation"""
        messages = []
        
        # Add P basics
        p_basics = self.resources.load_modular_context("p_basics.txt")
        messages.append(Message(
            role=MessageRole.USER,
            content=f"Reference P Language Guide:\n{p_basics}"
        ))
        
        # Add specific guides
        types_guide = self.resources.load_modular_context("p_types_guide.txt")
        events_guide = self.resources.load_modular_context("p_events_guide.txt")
        enums_guide = self.resources.load_modular_context("p_enums_guide.txt")
        
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<types_guide>\n{types_guide}\n</types_guide>"
        ))
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<events_guide>\n{events_guide}\n</events_guide>"
        ))
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<enums_guide>\n{enums_guide}\n</enums_guide>"
        ))
        
        # Add RAG context (similar type/event examples from corpus)
        rag_context = self._get_rag_context("types", "types and events", design_doc)
        if rag_context:
            messages.append(Message(
                role=MessageRole.USER,
                content=f"<similar_examples>\n{rag_context}\n</similar_examples>"
            ))
        
        # Add design doc
        messages.append(Message(
            role=MessageRole.USER,
            content=f"Design Document:\n{design_doc}"
        ))
        
        # Add instruction
        instruction = self.resources.load_instruction("generate_enums_types_events.txt")
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
        messages = []
        
        # Add guides
        p_basics = self.resources.load_modular_context("p_basics.txt")
        machines_guide = self.resources.load_modular_context("p_machines_guide.txt")
        
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<p_basics>\n{p_basics}\n</p_basics>"
        ))
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<machines_guide>\n{machines_guide}\n</machines_guide>"
        ))
        
        # Add RAG context (similar examples from corpus)
        rag_context = self._get_rag_context("machine", machine_name, design_doc)
        if rag_context:
            messages.append(Message(
                role=MessageRole.USER,
                content=f"<similar_examples>\n{rag_context}\n</similar_examples>"
            ))
        
        # Add context files
        if context_files:
            for filename, content in context_files.items():
                messages.append(Message(
                    role=MessageRole.USER,
                    content=f"<{filename}>\n{content}\n</{filename}>"
                ))
        
        # Add design doc
        messages.append(Message(
            role=MessageRole.USER,
            content=f"Design Document:\n{design_doc}"
        ))
        
        # Add instruction
        instruction = self.resources.load_instruction("generate_machine_structure.txt")
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
        messages = []
        
        # Add guides
        p_basics = self.resources.load_modular_context("p_basics.txt")
        machines_guide = self.resources.load_modular_context("p_machines_guide.txt")
        statements_guide = self.resources.load_modular_context("p_statements_guide.txt")
        
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<p_basics>\n{p_basics}\n</p_basics>"
        ))
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<machines_guide>\n{machines_guide}\n</machines_guide>"
        ))
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<statements_guide>\n{statements_guide}\n</statements_guide>"
        ))
        
        # Add context files
        if context_files:
            for filename, content in context_files.items():
                messages.append(Message(
                    role=MessageRole.USER,
                    content=f"<{filename}>\n{content}\n</{filename}>"
                ))
        
        # Add design doc
        messages.append(Message(
            role=MessageRole.USER,
            content=f"Design Document:\n{design_doc}"
        ))
        
        # Add instruction with optional structure
        instruction = self.resources.load_instruction("generate_machine.txt")
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
        p_basics = self.resources.load_modular_context("p_basics.txt")
        spec_guide = self.resources.load_modular_context("p_spec_monitors_guide.txt")
        
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<p_basics>\n{p_basics}\n</p_basics>"
        ))
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<spec_guide>\n{spec_guide}\n</spec_guide>"
        ))
        
        # Add context files
        if context_files:
            for filename, content in context_files.items():
                messages.append(Message(
                    role=MessageRole.USER,
                    content=f"<{filename}>\n{content}\n</{filename}>"
                ))
        
        # Add design doc
        messages.append(Message(
            role=MessageRole.USER,
            content=f"Design Document:\n{design_doc}"
        ))
        
        # Add instruction
        instruction = self.resources.load_instruction("generate_spec_files.txt")
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
        p_basics = self.resources.load_modular_context("p_basics.txt")
        test_guide = self.resources.load_modular_context("p_test_cases_guide.txt")
        
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<p_basics>\n{p_basics}\n</p_basics>"
        ))
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<test_guide>\n{test_guide}\n</test_guide>"
        ))
        
        # Add context files
        if context_files:
            for filename, content in context_files.items():
                messages.append(Message(
                    role=MessageRole.USER,
                    content=f"<{filename}>\n{content}\n</{filename}>"
                ))
        
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
        
        # Add design doc
        messages.append(Message(
            role=MessageRole.USER,
            content=f"Design Document:\n{design_doc}"
        ))
        
        # Add instruction
        instruction = self.resources.load_instruction("generate_test_files.txt")
        messages.append(Message(
            role=MessageRole.USER,
            content=instruction.format(filename=test_name)
        ))
        
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

            # Find start state
            start_state_match = re.search(
                r"start\s+state\s+(\w+)\s*\{(.*?)\n\s*\}",
                code,
                re.DOTALL,
            )
            if not start_state_match:
                continue
            start_state_name = start_state_match.group(1)
            start_body = start_state_match.group(2)

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

    def _extract_p_code(self, response: str, is_test_file: bool = False) -> tuple:
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
        
        Returns:
            Tuple of (filename, code) or (None, None) if not found
        """
        filename: Optional[str] = None
        code: Optional[str] = None

        # Strategy 1: XML-style tags  <Filename.p>...</Filename.p>
        xml_pattern = r'<(\w+\.p)>(.*?)</\1>'
        match = re.search(xml_pattern, response, re.DOTALL)
        if match:
            filename = match.group(1)
            code = match.group(2).strip()

        # Strategy 2: Markdown code block with a filename hint on the first line
        if not code:
            md_pattern = r'```(?:p)?\s*\n\s*//\s*(\w+\.p)\s*\n(.*?)```'
            match = re.search(md_pattern, response, re.DOTALL)
            if match:
                filename = match.group(1)
                code = match.group(2).strip()

        # Strategy 3: Bare markdown code block – derive filename from `machine` keyword
        if not code:
            md_bare = r'```(?:p)?\s*\n(.*?)```'
            match = re.search(md_bare, response, re.DOTALL)
            if match:
                candidate = match.group(1).strip()
                # Try to derive a filename from the first machine declaration
                name_match = re.search(r'\bmachine\s+(\w+)', candidate)
                if name_match:
                    filename = f"{name_match.group(1)}.p"
                    code = candidate

        # Strategy 4: No fences at all – look for `machine Foo {` blocks
        if not code:
            machine_block = re.search(
                r'(machine\s+\w+\s*\{.*)',
                response,
                re.DOTALL,
            )
            if machine_block:
                candidate = machine_block.group(1).strip()
                name_match = re.search(r'\bmachine\s+(\w+)', candidate)
                if name_match:
                    filename = f"{name_match.group(1)}.p"
                    code = candidate

        if not filename or not code:
            return None, None

        # Strip any remaining markdown fence artifacts
        code = re.sub(r'^```\w*\s*', '', code)
        code = re.sub(r'\s*```\s*$', '', code)

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
