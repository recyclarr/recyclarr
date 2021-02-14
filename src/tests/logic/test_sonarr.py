import pytest

from app.trash_error import TrashError
from app.logic import sonarr as logic
from app import cmd
from tests.mock_logger import MockLogger

class TestSonarrLogic:
    logger = MockLogger()

    @staticmethod
    def test_throw_without_required_arguments():
        with pytest.raises(TrashError):
            args = cmd.setup_and_parse_args(['profile', 'sonarr:anime', '--base-uri', 'value'])
            logic.process_profile(args, TestSonarrLogic.logger)

