# Development

General development notes.

## Release Instructions

1. Create new release branch `git flow release start vX.X.X`.
2. Finish release branch `git flow release finish vX.X.X`.
3. Checkout to main branch `git checkout main`.
4. Prepare new tag `git tag vX.X.X -a && git push --tags`.
5. Create new release `https://github.com/krasninja/querycat/releases/new`.
6. Create a publish with `./build.sh -t publish-all`.
7. Upload files.
8. Switch to develop branch `git checkout develop`.
9. Update CHANGELOG file.

## Run Documentation

We use MkDocs for static documentation generation and Read the Docs as hosting platform. To run the documentation site locally follow the steps below:

1. Make sure the [pyenv](https://github.com/pyenv/pyenv) and [pyenv-virtualenv](https://github.com/pyenv/pyenv-virtualenv) are installed.
2. Install Python 3.12 `pyenv install 3.12`.
3. Create virtual environment `pyenv virtualenv 3.12 qcat`.
4. Activate it `pyenv activate qcat`.
5. Being in the project root directory Install the required packages `pip install -r ./docs/requirements.txt`.
6. Start simple local web server `mkdocs serve`. The documentation will be available on http://127.0.0.1:8000/ address.

If the preparation steps have been already performed start with step 4 next time.
