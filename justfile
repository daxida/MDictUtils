run:
  dotnet run --project Cli -- assets/out2.mdx -a assets/stub.txt
  dotnet run --project Cli -- assets/out2.mdd -a assets/stub.txt

oracle:
  mdict assets/out1.mdx -a assets/stub.txt
  mdict assets/out1.mdd -a assets/stub.txt

oracle-undo:
  mdict -x assets/out1.mdx -d assets/undo

test:
  dotnet test Lib.Tests/

final-old:
  dotnet run --project Cli -- assets/out2.mdx -a assets/stub.txt
  mdict assets/out1.mdx -a assets/stub.txt
  cmp assets/out1.mdx assets/out2.mdx

final:
  @just run
  @just oracle
  cmp assets/out1.mdx assets/out2.mdx
  cmp assets/out1.mdd assets/out2.mdd

# Otherwise nvim go to definition sends you to assembly and not source code
sln:
  dotnet new sln -n MDictUtils --force
  dotnet sln MDictUtils.slnx add Cli/Cli.csproj
  dotnet sln MDictUtils.slnx add Lib/Lib.csproj
  dotnet sln MDictUtils.slnx add Lib.Tests/Lib.Tests.csproj
  dotnet build MDictUtils.slnx

alias r := run
alias t := test
