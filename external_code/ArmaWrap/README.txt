This a wrapper for Armadillo's truncated SVD function.

MathNET library's implementation runs with a n^2 runtime (n - rows) and is thus not usable for imputation, it's too slow for algorithms that rely on nlogn implementations of SVD (randomized SVD, econ SVD etc).

In case default installation (done by the install script) into /usr/lib/ doesn't work - the library file can be placed with the dotnet executable:
- run `make all` in this folder
- copy libArmaWrap.so to /CleanIMP/bin/Debug/net8.0/

NB: if the file is used locally - when .NET version is updated the file has to be copied to a new relevant /bin/Debug/netX.0/ folder for a different dotnet version. No need to rebuild, the same file can be used.
