rm -r -d MES.VFS~*
rm exVC3.exe
mcs -out:exVC3.exe extract.cs
mono exVC3.exe