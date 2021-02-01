# importer

docker run -v ~/repos/importer/importer/sample-data:/app/sample-data -t vietphan/importer:v01 -j /app/sample-data/drop-1.json

# SQL Server 

```shell
CREATE TABLE dbo.products
(
 sku nvarchar(100) NOT NULL,
 description nvarchar(1000) NOT NULL,
 category nvarchar(500) NOT NULL,
 price decimal(9,6)  NOT NULL,
 location nvarchar(500) NOT NULL,
 qty INT NOT NULL
);
GO

CREATE TABLE dbo.transmission
(
 id nvarchar(100) PRIMARY KEY DEFAULT NEWID(),
 recordcount INT NOT NULL,
 qtysum INT NOT NULL
);
GO

SELECT TOP (1000) [sku]
      ,[description]
      ,[category]
      ,[price]
      ,[location]
      ,[qty]
  FROM [DevOpsDB].[dbo].[products]

SELECT TOP (1000) [id]
      ,[recordcount]
      ,[qtysum]
  FROM [DevOpsDB].[dbo].[transmission]

DROP TABLE products;
DROP TABLE transmission;
```
