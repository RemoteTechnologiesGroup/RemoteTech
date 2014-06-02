#!/bin/bash

# These should be set by Travis
#TRAVIS_BUILD_NUMBER=1
#TRAVIS_BRANCH=master
#TRAVIS_REPO_SLUG="RemoteTechnologiesGroup/RemoteTech"
#TRAVIS_COMMIT=master
#GITHUB_TOKEN="Personal access token from https://github.com/settings/applications"
#TRAVIS_PULL_REQUEST=false

VERSION="build-${TRAVIS_BRANCH}-${TRAVIS_BUILD_NUMBER}"
FILENAME=$(echo "${VERSION}.zip" | tr '/' '_') # else it will fail on branches like chore/travis

python_parse_json() {
	# python errors are surpressed for when the key doesn't exist
	cat | python -c 'import sys,json;obj=json.load(sys.stdin);print obj[sys.argv[1]];' $1 2>/dev/null
}

if [ -z "$GITHUB_TOKEN" ] || [ -z "$TRAVIS_REPO_SLUG" ] \
	|| [ -z "$TRAVIS_BUILD_NUMBER" ] || [ -z "$TRAVIS_BRANCH" ] \
	|| [ -z "$TRAVIS_COMMIT" ]
then
	echo "GITHUB_TOKEN, TRAVIS_REPO_SLUG, TRAVIS_BUILD_NUMBER and TRAVIS_COMMIT must be set in order to deploy";
	echo "Skipping deploy for now";
	exit 0; # prevent build failing if unset
fi

if [ "$TRAVIS_PULL_REQUEST" != "false" ]
then
	echo "This is a pull request build, it doesn't need to be released."
	exit 0; # prevent build fail
fi

echo "Creating ${FILENAME}"
zip -r "${FILENAME}" GameData/

echo "Attempting to create tag ${VERSION} on ${TRAVIS_REPO_SLUG}"
API_JSON=$(printf '{"tag_name": "%s","target_commitish": "%s","name": "%s","body": "Automated pre-release of version %s","draft": false,"prerelease": true}' $VERSION $TRAVIS_COMMIT $VERSION $VERSION)
ADDRESS=$(printf 'https://api.github.com/repos/%s/releases?access_token=%s' $TRAVIS_REPO_SLUG $GITHUB_TOKEN)

REPLY=$(curl --data "$API_JSON" "$ADDRESS");
UPLOAD_ID=$(echo $REPLY | python_parse_json "id")
ERRORS=$(echo $REPLY | python_parse_json "errors");

if [ -n "$ERRORS" ] || [ -z "$REPLY" ] || [ -z "$UPLOAD_ID" ]
then
	echo "ERROR: An error occured while setting the tag";
	echo $REPLY;
	exit 1;
fi

UPLOAD_URL="https://uploads.github.com/repos/${TRAVIS_REPO_SLUG}/releases/${UPLOAD_ID}/assets"

echo "Uploading ${FILENAME} to GitHub repo ${UPLOAD_ID} (tag ${VERSION} on ${TRAVIS_REPO_SLUG})"
REPLY=$(curl -H "Authorization: token ${GITHUB_TOKEN}" \
     -H "Accept: application/vnd.github.manifold-preview" \
     -H "Content-Type: application/zip" \
     --data-binary @${FILENAME} \
     "${UPLOAD_URL}?name=${FILENAME}")

ERRORS=$(echo $REPLY | python_parse_json "errors")
ASSET_ID=$(echo $REPLY | python_parse_json "id" )

if [ -n "$ERRORS" ] || [ -z "$REPLY" ] || [ -z "$ASSET_ID" ]
then
	echo "ERROR: An error occured while uploading the file to GitHub";
	echo $REPLY;
	exit 1;
fi

echo "Uploaded ${FILENAME} to ${TRAVIS_REPO_SLUG} as asset id ${ASSET_ID}"
