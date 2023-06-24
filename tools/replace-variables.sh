#!/bin/bash

normalize_text() {
  local text="$1"
  # Remove special characters and replace with underscores
  text=${text//[^[:alnum:].]/_}
  # Remove multiple consecutive underscores
  text=${text//__/_}
  # Remove leading and trailing underscores
  text=${text/#_/}
  text=${text/%_/}
  # Convert to lowercase
  text=${text,,}
  echo "$text"
}

sanitize_variable() {
  local sanitized="$1"
  # Escape special characters for sed
  sanitized=${sanitized//\\/\\\\}   # Backslash
  sanitized=${sanitized//&/\\&}     # Ampersand
  sanitized=${sanitized//\//\\/}    # Slash
  sanitized=${sanitized//\$/\\\$}   # Dollar sign
  sanitized=${sanitized//\!/\\!}    # Exclamation mark
  sanitized=${sanitized//\./\\\.}   # Period
  sanitized=${sanitized//\*/\\\*}   # Asterisk
  sanitized=${sanitized//\[/\\\[}   # Opening square bracket
  sanitized=${sanitized//\]/\\\]}   # Closing square bracket
  sanitized=${sanitized//\^/\\\^}   # Caret
  sanitized=${sanitized//\$/\\\$}   # Dollar sign
  echo "$sanitized"
}

# Parse owner and repository name from the first argument
IFS='/' read -r REPOSITORY_OWNER REPOSITORY_NAME <<< "$1"

# Input variables
REPOSITORY_FULL_NAME="$1"
REPOSITORY_FULL_NAME_LOWER=$(normalize_text "$REPOSITORY_FULL_NAME")
REPOSITORY_OWNER_LOWER=$(normalize_text "$REPOSITORY_OWNER")
REPOSITORY_NAME="${REPOSITORY_NAME//[^[:alnum:].]/}"
REPOSITORY_NAME_LOWER=$(normalize_text "$REPOSITORY_NAME")

# Replace file names that contain @RepositoryName with the repository name
find . -name "*@RepositoryName*" -exec bash -c 'mv "$1" "${1//@RepositoryName/$2}"' _ {} "$REPOSITORY_NAME" \;

# Replace file contents with the repository variables
find . -type f -not -path "./tools/replace-variables.sh" -and -not -path "./.git/*" -exec sed -i "s/$(sanitize_variable '@RepositoryName')/$REPOSITORY_NAME/g" {} +
find . -type f -not -path "./tools/replace-variables.sh" -and -not -path "./.git/*" -exec sed -i "s/$(sanitize_variable '@RepositoryNameLower')/$REPOSITORY_NAME_LOWER/g" {} +
find . -type f -not -path "./tools/replace-variables.sh" -and -not -path "./.git/*" -exec sed -i "s/$(sanitize_variable '@RepositoryFullName')/$REPOSITORY_FULL_NAME/g" {} +
find . -type f -not -path "./tools/replace-variables.sh" -and -not -path "./.git/*" -exec sed -i "s/$(sanitize_variable '@RepositoryFullNameLower')/$REPOSITORY_FULL_NAME_LOWER/g" {} +
find . -type f -not -path "./tools/replace-variables.sh" -and -not -path "./.git/*" -exec sed -i "s/$(sanitize_variable '@RepositoryOwner')/$REPOSITORY_OWNER/g" {} +
find . -type f -not -path "./tools/replace-variables.sh" -and -not -path "./.git/*" -exec sed -i "s/$(sanitize_variable '@RepositoryOwnerLower')/$REPOSITORY_OWNER_LOWER/g" {} +

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
rm -rf .github/workflows/ && cp -rnT github/ .github/ && rm -rf github/ > /dev/null
rm -rf tools/replace-variables.sh > /dev/null