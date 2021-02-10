import pytest
import sys
from pathlib import Path

from app import cmd
from tests.mock_logger import MockLogger

sys.path.insert(0, Path(__name__).parent.parent)
import trash

class TestEntrypoint:
    logger = MockLogger()

    @staticmethod
    def test_throw_without_required_arguments():
        with pytest.raises(ValueError):
            args = cmd.setup_and_parse_args(['profile', 'sonarr:anime', '--base-uri', 'value'])
            trash.process_sonarr_profile(args, TestEntrypoint.logger)

