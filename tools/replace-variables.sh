#!/bin/bash

REPOSITORY_NAME=$1
REPOSITORY_DESCRIPTION=$2

# Replace file names that contain @RepositoryName with the repository name
find . -name "*\@RepositoryName*" -exec bash -c 'mv "$1" "${1/\@RepositoryName/$2}"' _ {} "$REPOSITORY_NAME" \;

# Replace file contents that contain @RepositoryName with the repository name
find . -type f -not -path "./tools/replace-variables.sh" -and -not -path "./.git/*" -exec sed -i "s/\@RepositoryName/$REPOSITORY_NAME/g" {} +
find . -type f -not -path "./tools/replace-variables.sh" -and -not -path "./.git/*" -exec sed -i "s/\@RepositoryDescription/$REPOSITORY_DESCRIPTION/g" {} +

# Look for unused variables
grep -rFl --exclude-dir=tools --exclude-dir=.git --exclude-dir=.github --exclude-dir=github "@" .
if [ $? -eq 0 ]; then
    echo "ERROR: Unused variables found."
    exit 1
fi

# Add all files to git
git config --global user.email "github-actions[bot]@users.noreply.github.com" > /dev/null
git config --global user.name "github-actions[bot]" > /dev/null
git add . > /dev/null
git commit -m "[ci-skip] Replace variables." > /dev/null

# Remove this script
rm -rf .github/workflows/ && cp -rn github/ .github/ && rm -rf github/ > /dev/null
rm -rf tools/replace-variables.sh > /dev/null