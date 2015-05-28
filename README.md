DatabaseDataLoader
==================

A tool that assists in loading data into a database using CSV files. The tool is simple, lightweight and clearly maps to your database to assist in database updates or seeding data.

### Basic Usage

`
DatabaseDataLoader.exe -baseDir <baseDirectory> -connection <connectionString>
`

The _baseDir_ argument specifies the base directory to load data from. This folder should contain two child directores:

* InsertOnly - Files in this directory are checked for the primary key existence and if the record exists it will not be written.
* Updateable - Files in this directory are either inserted or updated on every run.

The _connection_ argument should be a .NET connection string to the database that is loaded.

### How it Works

The utility connects to the database, and runs through each of the above folders. For each .csv file it encounters, it uses the file name to map to a table in the database. Then it reflects the primary key from the schema and uses that to determine if the record exists. Then based on the folder it inserts, updates or skips the record.

It is important to note that order doesn't matter here. Constraints and foreign keys are disabled during load and enabled once the load is complete.

The utility will log each file and indicate how many records have been inserted, updated or skipped. To support large data, currently transactions are not being used when loading data.


