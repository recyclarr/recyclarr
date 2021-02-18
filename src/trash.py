from pathlib import Path

from app.logic.main import main
from app.trash_error import TrashError

# --------------------------------------------------------------------------------------------------
if __name__ == '__main__':
    try:
        main(Path(__file__).parent)
    except TrashError as e:
        print(f'ERROR: {e}')
        exit(1)
