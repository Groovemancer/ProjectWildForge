FROM alpine:latest

LABEL repository="https://github.com/icedmetal57/ProjectWildForge"
LABEL "com.github.actions.name"="Merge Branches"
LABEL "com.github.actions.description"="Automatically merge from one branch to another."
LABEL "com.github.actions.icon"="git-merge"
LABEL "com.github.actions.color"="orange"

RUN apk --no-cache add bash curl git jq

ADD entrypoint.sh /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]