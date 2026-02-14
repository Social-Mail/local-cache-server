# Local Cache Server
Local Cache server creates memory/disk based cache to be used in Social Mail Web Server

# Why?
Well redis and other caching technologies do exist, however, this server may simply act as a simple cache and can be configured to use something else in distributed environment without having to change social mail web server code.

# Features
1. Simple JSON messaging. Line terminated JSON in and JSON out. Instead of HTTP in/out. HTTP requires unnecessary parsing, host, path mapping, unnecessary overheads.
2. Message can use disk based or memory based caching.
3. Dotnet's inbuilt concurrent dictionary based cache can be utilized efficiently.
