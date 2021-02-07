FROM python:3.8-slim

RUN mkdir /trash-updater

RUN apt-get update && apt-get install -y --no-install-recommends curl unzip

RUN cd /trash-updater && curl -LO https://github.com/rcdailey/trash-updater/archive/master.zip

RUN cd /trash-updater && unzip master.zip && cd trash-updater-master && pip install -r requirements.txt

CMD ["python3", "/trash-updater/trash-updater-master/trash.py", "--preview", "$SONARR_BASE_URL", "$SONARR_API_KEY"]
