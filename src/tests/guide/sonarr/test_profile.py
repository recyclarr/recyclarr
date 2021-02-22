from app.guide.sonarr import profile
from pathlib import Path
from tests.mock_logger import MockLogger

data_files = Path(__file__).parent / 'data'

def test_parse_markdown_complete_doc():
    md_file = data_files / 'test_parse_markdown_complete_doc.md'
    with open(md_file) as file:
        test_markdown = file.read()

    class args:
        strict_negative_scores = False

    results = profile.parse_markdown(args, MockLogger(), test_markdown)

    assert len(results) == 1
    test_profile = next(iter(results.values()))

    assert len(test_profile.ignored) == 2
    assert sorted(test_profile.ignored) == sorted(['term2', 'term3'])

    assert len(test_profile.required) == 1
    assert test_profile.required == ['term4']

    assert len(test_profile.preferred) == 1
    assert test_profile.preferred.get(100) == ['term1']


def test_parse_markdown_strict_negative_scores():
    test_markdown = '''
# Test Release Profile

This score is negative [-1]

```
abc
```

This score is positive [0]

```
xyz
```
'''

    class args:
        strict_negative_scores = True

    results = profile.parse_markdown(args, MockLogger(), test_markdown)
    assert len(results['Test Release Profile'].required) == 0
    assert len(results['Test Release Profile'].ignored) == 1
    assert results['Test Release Profile'].ignored[0] == 'abc'
    assert len(results['Test Release Profile'].preferred) == 1
    assert results['Test Release Profile'].preferred[0] == ['xyz']
