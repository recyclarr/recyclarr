import app.guide.quality as quality
from pathlib import Path
from tests.mock_logger import MockLogger

data_files = Path(__file__).parent / 'data'

def test_parse_markdown_complete_doc():
    md_file = data_files / 'test_parse_markdown_sonarr_quality_definitions.md'
    with open(md_file) as file:
        test_markdown = file.read()

    results = quality.parse_markdown(MockLogger(), test_markdown)

    # Dictionary: Key (header name (anime or non-anime)), list (quality definitions table rows)
    assert len(results) == 2

    table = results.get('sonarr:anime')
    assert len(table) == 14
    table_expected = [
        ('HDTV-720p', 2.3, 51.4),
        ('HDTV-1080p', 2.3, 100.0),
        ('WEBRip-720p', 4.3, 100.0),
        ('WEBDL-720p', 4.3, 51.4),
        ('Bluray-720p', 4.3, 102.2),
        ('WEBRip-1080p', 4.5, 257.4),
        ('WEBDL-1080p', 4.3, 253.6),
        ('Bluray-1080p', 4.3, 258.1),
        ('Bluray-1080p Remux', 0.0, 400.0),
        ('HDTV-2160p', 84.5, 350.0),
        ('WEBRip-2160p', 84.5, 350.0),
        ('WEBDL-2160p', 84.5, 350.0),
        ('Bluray-2160p', 94.6, 400.0),
        ('Bluray-2160p Remux', 204.4, 400.0)
    ]
    assert sorted(table) == sorted(table_expected)

    table = results.get('sonarr:non-anime')
    assert len(table) == 14
    table_expected = [
        ('HDTV-720p', 17.9, 67.5),
        ('HDTV-1080p', 20.0, 137.3),
        ('WEBRip-720p', 20.0, 137.3),
        ('WEBDL-720p', 20.0, 137.3),
        ('Bluray-720p', 34.9, 137.3),
        ('WEBRip-1080p', 22.0, 137.3),
        ('WEBDL-1080p', 22.0, 137.3),
        ('Bluray-1080p', 50.4, 227.0),
        ('Bluray-1080p Remux', 69.1, 400.0),
        ('HDTV-2160p', 84.5, 350.0),
        ('WEBRip-2160p', 84.5, 350.0),
        ('WEBDL-2160p', 84.5, 350.0),
        ('Bluray-2160p', 94.6, 400.0),
        ('Bluray-2160p Remux', 204.4, 400.0)
    ]
    assert sorted(table) == sorted(table_expected)
