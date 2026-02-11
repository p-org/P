"""
Input Validators for PeasyAI.

This module provides validators for user inputs:
- Design document validation
- Project path validation
- Configuration validation
"""

import os
import re
from dataclasses import dataclass
from typing import List, Optional, Tuple
from pathlib import Path


@dataclass
class InputValidationResult:
    """Result of input validation."""
    is_valid: bool
    errors: List[str]
    warnings: List[str]
    
    @classmethod
    def success(cls) -> "InputValidationResult":
        return cls(is_valid=True, errors=[], warnings=[])
    
    @classmethod
    def failure(cls, error: str) -> "InputValidationResult":
        return cls(is_valid=False, errors=[error], warnings=[])


class DesignDocValidator:
    """
    Validates design documents before processing.
    
    Checks for:
    - Required sections (title, components, interactions)
    - Minimum content length
    - Valid structure
    """
    
    # Required sections in a design document
    REQUIRED_SECTIONS = [
        "title",
        "component",
        "interaction",
    ]
    
    # Minimum document length (characters)
    MIN_LENGTH = 100
    
    # Maximum document length (characters)
    MAX_LENGTH = 100000
    
    def validate(self, content: str) -> InputValidationResult:
        """
        Validate a design document.
        
        Args:
            content: The design document content
            
        Returns:
            InputValidationResult with any issues
        """
        errors = []
        warnings = []
        
        # Check length
        if len(content) < self.MIN_LENGTH:
            errors.append(
                f"Design document is too short ({len(content)} chars). "
                f"Minimum is {self.MIN_LENGTH} characters."
            )
        
        if len(content) > self.MAX_LENGTH:
            errors.append(
                f"Design document is too long ({len(content)} chars). "
                f"Maximum is {self.MAX_LENGTH} characters."
            )
        
        # Check for required sections
        content_lower = content.lower()
        for section in self.REQUIRED_SECTIONS:
            if section not in content_lower:
                warnings.append(
                    f"Design document may be missing '{section}' section. "
                    f"Consider adding <{section}>...</{section}> tags."
                )
        
        # Check for machine/component definitions
        if not re.search(r"<component|machine|state\s+machine", content, re.IGNORECASE):
            warnings.append(
                "No clear component/machine definitions found. "
                "Consider using <component>MachineName</component> tags."
            )
        
        # Check for event definitions
        if not re.search(r"event|message|signal", content, re.IGNORECASE):
            warnings.append(
                "No event/message definitions found. "
                "Consider describing the events/messages exchanged."
            )
        
        return InputValidationResult(
            is_valid=len(errors) == 0,
            errors=errors,
            warnings=warnings
        )
    
    def extract_metadata(self, content: str) -> dict:
        """
        Extract metadata from a design document.
        
        Args:
            content: The design document content
            
        Returns:
            Dictionary with extracted metadata
        """
        metadata = {
            "title": None,
            "components": [],
            "events": [],
        }
        
        # Extract title
        title_match = re.search(r"<title>([^<]+)</title>", content, re.IGNORECASE)
        if title_match:
            metadata["title"] = title_match.group(1).strip()
        
        # Extract components
        component_pattern = r"<component[^>]*>([^<]+)</component>"
        for match in re.finditer(component_pattern, content, re.IGNORECASE):
            metadata["components"].append(match.group(1).strip())
        
        # Also look for bullet-point components
        bullet_pattern = r"[-*]\s*(\w+)\s*(?:machine|component|:)"
        for match in re.finditer(bullet_pattern, content, re.IGNORECASE):
            name = match.group(1).strip()
            if name not in metadata["components"] and name[0].isupper():
                metadata["components"].append(name)
        
        return metadata


class ProjectPathValidator:
    """
    Validates project paths.
    
    Checks for:
    - Path existence
    - Required P project structure
    - Write permissions
    """
    
    # Required directories for a P project
    REQUIRED_DIRS = ["PSrc", "PSpec", "PTst"]
    
    def validate_existing_project(self, path: str) -> InputValidationResult:
        """
        Validate an existing P project path.
        
        Args:
            path: Path to the P project
            
        Returns:
            InputValidationResult with any issues
        """
        errors = []
        warnings = []
        
        # Check if path exists
        if not os.path.exists(path):
            return InputValidationResult.failure(f"Path does not exist: {path}")
        
        if not os.path.isdir(path):
            return InputValidationResult.failure(f"Path is not a directory: {path}")
        
        # Check for required directories
        for dir_name in self.REQUIRED_DIRS:
            dir_path = os.path.join(path, dir_name)
            if not os.path.exists(dir_path):
                warnings.append(f"Missing directory: {dir_name}")
        
        # Check for .pproj file
        pproj_files = [f for f in os.listdir(path) if f.endswith(".pproj")]
        if not pproj_files:
            warnings.append("No .pproj file found in project root")
        
        # Check for at least one .p file
        p_files_found = False
        for dir_name in self.REQUIRED_DIRS:
            dir_path = os.path.join(path, dir_name)
            if os.path.exists(dir_path):
                p_files = [f for f in os.listdir(dir_path) if f.endswith(".p")]
                if p_files:
                    p_files_found = True
                    break
        
        if not p_files_found:
            warnings.append("No .p files found in project")
        
        return InputValidationResult(
            is_valid=len(errors) == 0,
            errors=errors,
            warnings=warnings
        )
    
    def validate_output_path(self, path: str) -> InputValidationResult:
        """
        Validate a path for creating a new project.
        
        Args:
            path: Path where project will be created
            
        Returns:
            InputValidationResult with any issues
        """
        errors = []
        warnings = []
        
        # Check if parent directory exists
        parent = os.path.dirname(path)
        if parent and not os.path.exists(parent):
            # Try to check if we can create it
            try:
                os.makedirs(parent, exist_ok=True)
            except PermissionError:
                return InputValidationResult.failure(
                    f"Cannot create directory: {parent} (permission denied)"
                )
            except Exception as e:
                return InputValidationResult.failure(
                    f"Cannot create directory: {parent} ({e})"
                )
        
        # Check if path already exists
        if os.path.exists(path):
            if os.listdir(path):
                warnings.append(
                    f"Directory already exists and is not empty: {path}"
                )
        
        # Check write permissions
        test_path = path if os.path.exists(path) else parent or "."
        if not os.access(test_path, os.W_OK):
            return InputValidationResult.failure(
                f"No write permission for: {test_path}"
            )
        
        return InputValidationResult(
            is_valid=len(errors) == 0,
            errors=errors,
            warnings=warnings
        )
    
    def get_project_files(self, path: str) -> dict:
        """
        Get all P files in a project.
        
        Args:
            path: Path to the P project
            
        Returns:
            Dictionary mapping relative paths to file contents
        """
        files = {}
        
        for dir_name in self.REQUIRED_DIRS:
            dir_path = os.path.join(path, dir_name)
            if os.path.exists(dir_path):
                for filename in os.listdir(dir_path):
                    if filename.endswith(".p"):
                        file_path = os.path.join(dir_path, filename)
                        rel_path = os.path.join(dir_name, filename)
                        try:
                            with open(file_path, "r") as f:
                                files[rel_path] = f.read()
                        except Exception:
                            pass
        
        return files
