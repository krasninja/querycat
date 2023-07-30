Smoke Test Plan
===============

1. Plugins.
    - List/Install/Remove.
    - Update.
2. Stdout processing:
    - `ps -ef | ./qcat "select *"`.
3. Try to use any plugin to check that key columns are working fine.
4. Make sure web server is working `qcat serve`.
5. Briefly check commands `explain`, `schema`, `ast`.
