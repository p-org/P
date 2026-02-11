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
    """
    
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
            
            # Extract code from response
            filename, code = self._extract_p_code(response.content)
            
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
                    error="Could not extract P code from response",
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
            
            # Extract code
            filename, code = self._extract_p_code(response.content)
            
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
                    error="Could not extract P code from response",
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
                    error="Could not extract P code from response",
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
            
            filename, code = self._extract_p_code(response.content)
            
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
                    error="Could not extract P code from response",
                    raw_response=response.content,
                )
                
        except Exception as e:
            logger.error(f"Error generating test {test_name}: {e}")
            return GenerationResult(
                success=False,
                error=str(e),
            )
    
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
    
    def _extract_p_code(self, response: str) -> tuple:
        """
        Extract P code from LLM response.
        
        Tries multiple extraction strategies in order:
        1. XML-style <filename.p>...</filename.p> tags
        2. Markdown code block with filename comment  (```p // filename.p ...)
        3. Markdown code block (```p ... ```) using first `machine` name
        
        Also post-processes the code to fix common issues.
        
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
            result = processor.process(code, filename)
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
