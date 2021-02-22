import pytest

from app import cmd
from app.logic import radarr
from app.trash_error import TrashError
from tests.mock_logger import MockLogger

@pytest.mark.parametrize('percentage', ['-1', '101'])
def test_process_quality_bad_preferred_percentage(percentage):
    input_args = ['quality', 'radarr:movies', '--preferred-percentage', percentage]
    args = cmd.setup_and_parse_args(input_args)
    with pytest.raises(TrashError):
        radarr.process_quality(args, MockLogger())