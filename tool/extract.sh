rm -r -d MES.VFS~*
rm extractVC3.exe
mcs -out:extractVC3.exe extract.cs
mono extractVC3.exe