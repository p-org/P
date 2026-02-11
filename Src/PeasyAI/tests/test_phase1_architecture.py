#!/usr/bin/env python3
"""
Phase 1 Architecture Tests

Tests for the new LLM provider abstraction and services layer.
"""

import os
import sys
import unittest
from pathlib import Path
from unittest.mock import Mock, patch, MagicMock

# Add project root to path
PROJECT_ROOT = Path(__file__).parent.parent
sys.path.insert(0, str(PROJECT_ROOT / "src"))

from core.llm.base import (
    LLMProvider,
    LLMConfig,
    LLMResponse,
    Message,
    MessageRole,
    Document,
    TokenUsage,
)
from core.llm.factory import LLMProviderFactory, get_default_provider, reset_default_provider
from core.services.base import EventCallback, ResourceLoader, BaseService
from core.services.generation import GenerationService, GenerationResult
from core.services.compilation import CompilationService, CompilationResult, ParsedError
from core.services.fixer import FixerService, FixResult, FixAttemptTracker


class TestLLMDataModels(unittest.TestCase):
    """Test LLM data model classes"""
    
    def test_message_creation(self):
        """Test Message creation and conversion"""
        msg = Message(role=MessageRole.USER, content="Hello")
        self.assertEqual(msg.role, MessageRole.USER)
        self.assertEqual(msg.content, "Hello")
        self.assertIsNone(msg.documents)
        
        d = msg.to_dict()
        self.assertEqual(d["role"], "user")
        self.assertEqual(d["content"], "Hello")
    
    def test_message_with_documents(self):
        """Test Message with document attachments"""
        doc = Document(name="test", content="Test content")
        msg = Message(role=MessageRole.USER, content="See attached", documents=[doc])
        
        full_content = msg.get_full_content()
        self.assertIn("See attached", full_content)
        self.assertIn("<test>", full_content)
        self.assertIn("Test content", full_content)
    
    def test_llm_config_defaults(self):
        """Test LLMConfig default values"""
        config = LLMConfig()
        self.assertEqual(config.max_tokens, 4096)
        self.assertEqual(config.temperature, 1.0)
        self.assertEqual(config.top_p, 0.999)
        self.assertEqual(config.timeout, 600.0)
    
    def test_token_usage(self):
        """Test TokenUsage conversion"""
        usage = TokenUsage(input_tokens=100, output_tokens=50, total_tokens=150)
        d = usage.to_dict()
        self.assertEqual(d["inputTokens"], 100)
        self.assertEqual(d["outputTokens"], 50)
        self.assertEqual(d["totalTokens"], 150)
    
    def test_llm_response(self):
        """Test LLMResponse conversion to legacy format"""
        usage = TokenUsage(input_tokens=100, output_tokens=50, total_tokens=150)
        response = LLMResponse(
            content="Hello world",
            usage=usage,
            finish_reason="stop",
            latency_ms=500,
            model="claude-3-5-sonnet",
            provider="test"
        )
        
        d = response.to_dict()
        self.assertEqual(d["output"]["message"]["content"][0]["text"], "Hello world")
        self.assertEqual(d["stopReason"], "stop")
        self.assertEqual(d["usage"]["inputTokens"], 100)


class TestLLMProviderFactory(unittest.TestCase):
    """Test LLM provider factory"""
    
    def setUp(self):
        reset_default_provider()
        # Clear relevant env vars
        for var in ["LLM_PROVIDER", "OPENAI_BASE_URL", "OPENAI_API_KEY",
                    "ANTHROPIC_API_KEY", "ANTHROPIC_BASE_URL"]:
            if var in os.environ:
                del os.environ[var]
    
    def tearDown(self):
        reset_default_provider()
    
    def test_create_unknown_provider(self):
        """Test creating unknown provider raises error"""
        with self.assertRaises(ValueError) as ctx:
            LLMProviderFactory.create("unknown_provider", {})
        self.assertIn("Unknown provider", str(ctx.exception))
    
    @patch.dict(os.environ, {
        "OPENAI_API_KEY": "test-key",
        "OPENAI_BASE_URL": "https://test.snowflakecomputing.com/api"
    })
    def test_auto_detect_snowflake(self):
        """Test auto-detection of Snowflake Cortex"""
        provider = LLMProviderFactory.from_env()
        self.assertEqual(provider.name, "snowflake_cortex")
    
    @patch.dict(os.environ, {"ANTHROPIC_API_KEY": "test-key"}, clear=True)
    def test_auto_detect_anthropic(self):
        """Test auto-detection of Anthropic"""
        # Clear snowflake vars
        os.environ.pop("OPENAI_BASE_URL", None)
        provider = LLMProviderFactory.from_env()
        self.assertEqual(provider.name, "anthropic")
    
    def test_explicit_provider_selection(self):
        """Test explicit provider selection via env var"""
        with patch.dict(os.environ, {
            "LLM_PROVIDER": "bedrock",
            "AWS_REGION": "us-west-2"
        }):
            provider = LLMProviderFactory.from_env()
            self.assertEqual(provider.name, "bedrock")


class TestEventCallback(unittest.TestCase):
    """Test event callback system"""
    
    def test_default_callbacks_use_logger(self):
        """Test that default callbacks don't raise exceptions"""
        cb = EventCallback()
        # These should not raise
        cb.status("Test status")
        cb.progress("Step", 1, 10)
        cb.error("Test error")
        cb.warning("Test warning")
    
    def test_custom_callbacks(self):
        """Test custom callback functions"""
        statuses = []
        errors = []
        
        cb = EventCallback(
            on_status=lambda msg: statuses.append(msg),
            on_error=lambda msg: errors.append(msg)
        )
        
        cb.status("Status 1")
        cb.status("Status 2")
        cb.error("Error 1")
        
        self.assertEqual(statuses, ["Status 1", "Status 2"])
        self.assertEqual(errors, ["Error 1"])


class TestResourceLoader(unittest.TestCase):
    """Test resource loader"""
    
    def setUp(self):
        self.loader = ResourceLoader(PROJECT_ROOT / "resources")
    
    def test_load_existing_file(self):
        """Test loading an existing resource file"""
        # This file should exist
        try:
            content = self.loader.load_context("about_p.txt")
            self.assertIsInstance(content, str)
            self.assertGreater(len(content), 0)
        except FileNotFoundError:
            self.skipTest("Resource file not found")
    
    def test_load_nonexistent_file(self):
        """Test loading non-existent file raises error"""
        with self.assertRaises(FileNotFoundError):
            self.loader.load("nonexistent_file_12345.txt")
    
    def test_cache_behavior(self):
        """Test that caching works"""
        try:
            # Load twice
            content1 = self.loader.load_context("about_p.txt")
            content2 = self.loader.load_context("about_p.txt")
            self.assertEqual(content1, content2)
            
            # Clear cache and reload
            self.loader.clear_cache()
            content3 = self.loader.load_context("about_p.txt")
            self.assertEqual(content1, content3)
        except FileNotFoundError:
            self.skipTest("Resource file not found")


class TestFixAttemptTracker(unittest.TestCase):
    """Test fix attempt tracking"""
    
    def test_add_and_count_attempts(self):
        """Test adding and counting attempts"""
        tracker = FixAttemptTracker(max_attempts=3)
        
        self.assertEqual(tracker.get_attempt_count("error1"), 0)
        
        tracker.add_attempt("error1", "Attempt 1")
        self.assertEqual(tracker.get_attempt_count("error1"), 1)
        
        tracker.add_attempt("error1", "Attempt 2")
        self.assertEqual(tracker.get_attempt_count("error1"), 2)
    
    def test_should_request_guidance(self):
        """Test guidance request threshold"""
        tracker = FixAttemptTracker(max_attempts=2)
        
        self.assertFalse(tracker.should_request_guidance("error1"))
        
        tracker.add_attempt("error1", "Attempt 1")
        self.assertFalse(tracker.should_request_guidance("error1"))
        
        tracker.add_attempt("error1", "Attempt 2")
        self.assertTrue(tracker.should_request_guidance("error1"))
    
    def test_clear_attempts(self):
        """Test clearing attempts"""
        tracker = FixAttemptTracker()
        
        tracker.add_attempt("error1", "Attempt 1")
        tracker.add_attempt("error1", "Attempt 2")
        self.assertEqual(tracker.get_attempt_count("error1"), 2)
        
        tracker.clear("error1")
        self.assertEqual(tracker.get_attempt_count("error1"), 0)
    
    def test_get_attempts(self):
        """Test getting attempt descriptions"""
        tracker = FixAttemptTracker()
        
        tracker.add_attempt("error1", "First try")
        tracker.add_attempt("error1", "Second try")
        
        attempts = tracker.get_attempts("error1")
        self.assertEqual(attempts, ["First try", "Second try"])


class TestCompilationService(unittest.TestCase):
    """Test compilation service"""
    
    def test_parse_error(self):
        """Test parsing compilation error messages"""
        service = CompilationService()
        
        output = """
Building project...
/path/to/file.p(10, 5): error: undefined event 'eTest'
Build failed.
"""
        
        error = service.parse_error(output)
        self.assertIsNotNone(error)
        self.assertEqual(error.file_path, "/path/to/file.p")
        self.assertEqual(error.line_number, 10)
        self.assertEqual(error.column_number, 5)
        self.assertIn("undefined event", error.message)
    
    def test_parse_no_error(self):
        """Test parsing output with no errors"""
        service = CompilationService()
        
        output = "Build succeeded."
        error = service.parse_error(output)
        self.assertIsNone(error)
    
    def test_get_all_errors(self):
        """Test getting all errors from output"""
        service = CompilationService()
        
        output = """
/path/file1.p(10, 5): error: error 1
/path/file2.p(20, 10): error: error 2
/path/file3.p(30, 15): warning: warning 1
"""
        
        errors = service.get_all_errors(output)
        self.assertEqual(len(errors), 3)
        self.assertEqual(errors[0].file_path, "/path/file1.p")
        self.assertEqual(errors[1].file_path, "/path/file2.p")
        self.assertEqual(errors[2].error_type, "warning")


class TestMockLLMProvider(unittest.TestCase):
    """Test services with mocked LLM provider"""
    
    def setUp(self):
        self.mock_provider = Mock(spec=LLMProvider)
        self.mock_provider.name = "mock"
        self.mock_provider.default_model = "mock-model"
        
        # Setup default response
        self.mock_response = LLMResponse(
            content="<Test.p>\nevent eTest;\n</Test.p>",
            usage=TokenUsage(input_tokens=100, output_tokens=50, total_tokens=150),
            finish_reason="stop",
            latency_ms=500,
            model="mock-model",
            provider="mock"
        )
        self.mock_provider.complete.return_value = self.mock_response
    
    def test_generation_service_with_mock(self):
        """Test GenerationService with mocked LLM"""
        service = GenerationService(llm_provider=self.mock_provider)
        
        # Verify provider is used
        self.assertEqual(service.llm, self.mock_provider)


class TestServiceIntegration(unittest.TestCase):
    """Integration tests for services"""
    
    def test_services_share_provider(self):
        """Test that services can share an LLM provider"""
        mock_provider = Mock(spec=LLMProvider)
        mock_provider.name = "shared"
        
        gen_service = GenerationService(llm_provider=mock_provider)
        comp_service = CompilationService(llm_provider=mock_provider)
        fix_service = FixerService(llm_provider=mock_provider)
        
        # All should use the same provider
        self.assertEqual(gen_service.llm, mock_provider)
        self.assertEqual(comp_service.llm, mock_provider)
        self.assertEqual(fix_service.llm, mock_provider)


if __name__ == '__main__':
    unittest.main(verbosity=2)


