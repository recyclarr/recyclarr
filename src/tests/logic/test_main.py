import sys
from pathlib import Path

from app.logic import main

def test_main_sonarr_profile(mocker):
    test_args = ['trash.py', 'profile', 'sonarr:anime']
    mock_processor = mocker.patch('app.logic.sonarr.process_profile')
    mocker.patch.object(sys, 'argv', test_args)

    main.main(Path())

    mock_processor.assert_called_once()

def test_main_sonarr_quality(mocker):
    test_args = ['trash.py', 'quality', 'sonarr:anime']
    mock_processor = mocker.patch('app.logic.sonarr.process_quality')
    mocker.patch.object(sys, 'argv', test_args)

    main.main(Path())

    mock_processor.assert_called_once()