@echo off
set THISDIR=%~dp0
set SQLITE3=%THISDIR%sqlite3.exe
set SCRIPT=%THISDIR%quartz_table_sqlite.sql
set DB=%THISDIR%quartz-job-store.db
cat %SCRIPT% | %SQLITE3% %DB%
echo generated %DB%