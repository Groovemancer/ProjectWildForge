#!/bin/bash

set -e

echo
echo "  'Nightly Merge Action' is using the following input:"
echo "    - source_branch = '$INPUT_SOURCE_BRANCH'"
echo "    - target_branch = '$INPUT_TARGET_BRANCH'"
echo "    - allow_ff = $INPUT_ALLOW_FF"
echo "    - allow_git_lfs = $INPUT_GIT_LFS"
echo "    - ff_only = $INPUT_FF_ONLY"
echo "    - allow_forks = $INPUT_ALLOW_FORKS"
echo "    - user_name = $INPUT_USER_NAME"
echo "    - user_email = $INPUT_USER_EMAIL"
echo "    - push_token = $INPUT_PUSH_TOKEN = ${!INPUT_PUSH_TOKEN}"
echo

if [[ -z "${!INPUT_PUSH_TOKEN}" ]]; then
  echo "Set the ${INPUT_PUSH_TOKEN} env variable."
  exit 1
fi

FF_MODE="--no-ff"
if $INPUT_ALLOW_FF; then
  FF_MODE="--ff"
  if $INPUT_FF_ONLY; then
    FF_MODE="--ff-only"
  fi
fi

if ! $INPUT_ALLOW_FORKS; then
  if [[ -z "$GITHUB_TOKEN" ]]; then
    echo "Set the GITHUB_TOKEN env variable."
    exit 1
  fi
  URI=https://api.github.com
  API_HEADER="Accept: application/vnd.github.v3+json"
  AUTH_HEADER="Authorization: token $GITHUB_TOKEN"
  pr_resp=$(curl -X GET -s -H "${AUTH_HEADER}" -H "${API_HEADER}" "${URI}/repos/$GITHUB_REPOSITORY")
  if [[ "$(echo "$pr_resp" | jq -r .fork)" != "false" ]]; then
    echo "Nightly merge action is disabled for forks (use the 'allow_forks' option to enable it)."
    exit 0
  fi
fi

git clone https://x-access-token:${!INPUT_PUSH_TOKEN}@github.com/$GITHUB_REPOSITORY.git
cd docs
git config --global user.name "$INPUT_USER_NAME"
git config --global user.email "$INPUT_USER_EMAIL"

set -o xtrace

git checkout $INPUT_TARGET_BRANCH

# Do the merge
git merge origin/$INPUT_SOURCE_BRANCH

# Push the branch
git push