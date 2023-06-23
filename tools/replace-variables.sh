#!/bin/bash

REPOSITORY_NAME=$1

# Replace file names that contain $RepositoryName with the repository name
find . -name "$RepositoryName" -exec bash -c 'mv "$1" "${1/\$RepositoryName/$2}"' _ {} "$REPOSITORY_NAME" \;

# Replace file contents that contain $RepositoryName with the repository name
find . -type f ! -path "./tools/replace-variables.sh" -exec sed -i "s/\$RepositoryName/$REPOSITORY_NAME/g" {} +

# Add all files to git
git config --global user.email "github-actions[bot]@users.noreply.github.com"
git config --global user.name "github-actions[bot]"
git add . > /dev/null
git commit -m "[ci-skip] Replace variables." > /dev/null

# Remove this script
rm -rf .github/workflows/new-template.yml && mv .github/workflows/* .github/workflows/
rm -rf tools/replace-variables.sh