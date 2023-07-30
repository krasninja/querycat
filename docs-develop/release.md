Release Instructions
====================

1. Create new release branch `git flow release start vX.X.X`.
2. Finish release branch `git flow release finish vX.X.X`.
3. Checkout to main branch `git checkout main`.
4. Prepare new tag `git tag vX.X.X -a && git push --tags`.
5. Create new release `https://github.com/krasninja/querycat/releases/new`.
6. Create a publish with `./build.sh -t publish-all`.
7. Upload files.
8. Switch to develop branch `git checkout develop`.
9. Update CHANGELOG file.
