using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Tabliq.RemoteExecuter;
using Tabliq.Sql.Binding;

namespace Tabliq.Tests.RemoteExecuter;


public class TestConfigSchema
{
    private static readonly Lazy<DatabaseConnectionOptions> _schema;
    private static readonly Lazy<DatabaseConnectionOptions> _schemaFriendlyNames;
    private static readonly Lazy<VirtualSchema> _schemaFriendlyNamesVirtualSchema;

    public static DatabaseConnectionOptions Schema => _schema.Value;

    public static DatabaseConnectionOptions SchemaFriendlyNames => _schemaFriendlyNames.Value;

    public static VirtualSchema SchemaFriendlyNamesSchema => _schemaFriendlyNamesVirtualSchema.Value;

    static TestConfigSchema()
    {
        _schemaFriendlyNamesVirtualSchema = new Lazy<VirtualSchema>(() => new VirtualSchema
        {
            Tables = SchemaFriendlyNames.Tables.Select(x => x.AsVirtualTable()).ToList()
        });

        _schema = new Lazy<DatabaseConnectionOptions>(() => JsonSerializer.Deserialize<DatabaseConnectionOptions>("""
                {
                    "ConnectionString": "Server=sql; Database=ANON_Ingest; User Id=sa; Password=S3cr3tP@ssw0rd5;TrustServerCertificate=True",
                    "Tables": [
                        {
                            "Name": "filter_Table",
                            "RemoteTableSql": "SELECT AAA, BBB, CCC FROM FOO WHERE AAA = 'test'",
                            "Description": "Best Practice Rules",
                            "Columns": [
                                {
                                    "Name": "Col1",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "AAA",
                                    "Description": ""
                                },
                                {
                                    "Name": "Col2",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "BBB",
                                    "Description": "BP Rule ID"
                                },
                                {
                                    "Name": "Col3",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "CC",
                                    "Description": "BP Rule ID"
                                }
                            ]
                        },
                        {
                            "Name": "BE",
                            "RemoteTableSql": "landscapeQuery_strategy_A.BE",
                            "Description": "Best Practice Rules",
                            "Columns": [
                                {
                                    "Name": "BEId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "BEId",
                                    "Description": ""
                                },
                                {
                                    "Name": "BE_BPR_Alias",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "BE_BPR",
                                    "Description": "BP Rule ID"
                                },
                                {
                                    "Name": "BE_BPR",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "BE_BPR",
                                    "Description": "BP Rule ID"
                                },
                                {
                                    "Name": "BE_CAV",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "BE_CAV",
                                    "Description": "Caveat"
                                },
                                {
                                    "Name": "BE_COR",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "BE_COR",
                                    "Description": "Corrective Action"
                                },
                                {
                                    "Name": "BE_CRE",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "BE_CRE",
                                    "Description": "Created Date"
                                },
                                {
                                    "Name": "BE_DES",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "BE_DES",
                                    "Description": "Description"
                                },
                                {
                                    "Name": "BE_OST",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "BE_OST",
                                    "Description": "OS Type"
                                },
                                {
                                    "Name": "BE_PRI",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "BE_PRI",
                                    "Description": "Primary Technology"
                                },
                                {
                                    "Name": "BE_REC",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "BE_REC",
                                    "Description": "Recommendation"
                                },
                                {
                                    "Name": "BE_RIS",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "BE_RIS",
                                    "Description": "Risk"
                                },
                                {
                                    "Name": "BE_RUL",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "BE_RUL",
                                    "Description": "Rule Title"
                                },
                                {
                                    "Name": "BE_SEC",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "BE_SEC",
                                    "Description": "Secondary Technology"
                                }
                            ]
                        },
                        {
                            "Name": "BE_ES",
                            "RemoteTableSql": "landscapeQuery_strategy_A.BE_ES",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "BEId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "BEId",
                                    "Description": ""
                                },
                                {
                                    "Name": "ESId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "ESId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "CO",
                            "RemoteTableSql": "landscapeQuery_strategy_A.CO",
                            "Description": "Contract List",
                            "Columns": [
                                {
                                    "Name": "COId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "COId",
                                    "Description": ""
                                },
                                {
                                    "Name": "CO_ONT",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "CO_ONT",
                                    "Description": "Contract Number"
                                }
                            ]
                        },
                        {
                            "Name": "CO_PH",
                            "RemoteTableSql": "landscapeQuery_strategy_A.CO_PH",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "COId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "COId",
                                    "Description": ""
                                },
                                {
                                    "Name": "PHId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "PHId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "CO_SE",
                            "RemoteTableSql": "landscapeQuery_strategy_A.CO_SE",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "COId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "COId",
                                    "Description": ""
                                },
                                {
                                    "Name": "SEId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "SEId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "DE",
                            "RemoteTableSql": "landscapeQuery_strategy_A.DE",
                            "Description": "Defects",
                            "Columns": [
                                {
                                    "Name": "DEId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "DEId",
                                    "Description": ""
                                },
                                {
                                    "Name": "DE_CTF",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "DE_CTF",
                                    "Description": "Defect Fixed Version"
                                },
                                {
                                    "Name": "DE_DEF",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "DE_DEF",
                                    "Description": "Defect Number"
                                },
                                {
                                    "Name": "DE_ECT",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "DE_ECT",
                                    "Description": "Defect Reported Version"
                                },
                                {
                                    "Name": "DE_EFE",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "DE_EFE",
                                    "Description": "Defect Title"
                                },
                                {
                                    "Name": "DE_FEC",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "DE_FEC",
                                    "Description": "Defect Submitted On"
                                }
                            ]
                        },
                        {
                            "Name": "DE_SE",
                            "RemoteTableSql": "landscapeQuery_strategy_A.DE_SE",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "DEId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "DEId",
                                    "Description": ""
                                },
                                {
                                    "Name": "SEId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "SEId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "EC",
                            "RemoteTableSql": "landscapeQuery_strategy_A.EC",
                            "Description": "Technologies",
                            "Columns": [
                                {
                                    "Name": "ECId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "ECId",
                                    "Description": ""
                                },
                                {
                                    "Name": "EC_DIS",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "EC_DIS",
                                    "Description": "Disruption Index"
                                },
                                {
                                    "Name": "EC_TEC",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "EC_TEC",
                                    "Description": "Technology Name"
                                }
                            ]
                        },
                        {
                            "Name": "EC_PH",
                            "RemoteTableSql": "landscapeQuery_strategy_A.EC_PH",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "ECId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "ECId",
                                    "Description": ""
                                },
                                {
                                    "Name": "PHId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "PHId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "ES",
                            "RemoteTableSql": "landscapeQuery_strategy_A.ES",
                            "Description": "Best Practice Exceptions",
                            "Columns": [
                                {
                                    "Name": "ESId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "ESId",
                                    "Description": ""
                                },
                                {
                                    "Name": "ES_BPR",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "ES_BPR",
                                    "Description": "bpRuleID"
                                },
                                {
                                    "Name": "ES_DEV",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "ES_DEV",
                                    "Description": "Device ID"
                                },
                                {
                                    "Name": "ES_UNI",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "ES_UNI",
                                    "Description": "Unique ID"
                                }
                            ]
                        },
                        {
                            "Name": "ES_PH",
                            "RemoteTableSql": "landscapeQuery_strategy_A.ES_PH",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "ESId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "ESId",
                                    "Description": ""
                                },
                                {
                                    "Name": "PHId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "PHId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "EV",
                            "RemoteTableSql": "landscapeQuery_strategy_A.EV",
                            "Description": "Device History",
                            "Columns": [
                                {
                                    "Name": "EVId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "EVId",
                                    "Description": ""
                                },
                                {
                                    "Name": "EV_EOX",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "EV_EOX",
                                    "Description": "EOX Milestone"
                                },
                                {
                                    "Name": "EV_FRO",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "EV_FRO",
                                    "Description": "Historic Month"
                                },
                                {
                                    "Name": "EV_OPE",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "EV_OPE",
                                    "Description": "Operational Status"
                                },
                                {
                                    "Name": "EV_PSI",
                                    "DataType": "int",
                                    "RemoteColumnSql": "EV_PSI",
                                    "Description": "PSIRT Count"
                                },
                                {
                                    "Name": "EV_SHI",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "EV_SHI",
                                    "Description": "Shipped Date"
                                },
                                {
                                    "Name": "EV_SRS",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "EV_SRS",
                                    "Description": "SRs as % of IB"
                                },
                                {
                                    "Name": "EV_SWC",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "EV_SWC",
                                    "Description": "SW Conformance"
                                },
                                {
                                    "Name": "EV_TOT",
                                    "DataType": "int",
                                    "RemoteColumnSql": "EV_TOT",
                                    "Description": "Total SRs on active Devices"
                                },
                                {
                                    "Name": "EV_UID",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "EV_UID",
                                    "Description": "Unique Key"
                                }
                            ]
                        },
                        {
                            "Name": "EV_PH",
                            "RemoteTableSql": "landscapeQuery_strategy_A.EV_PH",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "EVId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "EVId",
                                    "Description": ""
                                },
                                {
                                    "Name": "PHId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "PHId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "JP",
                            "RemoteTableSql": "landscapeQuery_strategy_A.JP",
                            "Description": "Business Unit",
                            "Columns": [
                                {
                                    "Name": "JPId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "JPId",
                                    "Description": ""
                                },
                                {
                                    "Name": "JP_DIS",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "JP_DIS",
                                    "Description": "Disruption Index"
                                },
                                {
                                    "Name": "JP_SER",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "JP_SER",
                                    "Description": "Unit Name"
                                }
                            ]
                        },
                        {
                            "Name": "JP_PH",
                            "RemoteTableSql": "landscapeQuery_strategy_A.JP_PH",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "JPId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "JPId",
                                    "Description": ""
                                },
                                {
                                    "Name": "PHId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "PHId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "MA",
                            "RemoteTableSql": "landscapeQuery_strategy_A.MA",
                            "Description": "Shipments \u0026 Returns",
                            "Columns": [
                                {
                                    "Name": "MAId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "MAId",
                                    "Description": ""
                                },
                                {
                                    "Name": "MA_BOO",
                                    "DataType": "int",
                                    "RemoteColumnSql": "MA_BOO",
                                    "Description": "Boomerang Line Flag"
                                },
                                {
                                    "Name": "MA_HIP",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "MA_HIP",
                                    "Description": "Ship-To Contact Name"
                                },
                                {
                                    "Name": "MA_INE",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "MA_INE",
                                    "Description": "Line ID"
                                },
                                {
                                    "Name": "MA_LAB",
                                    "DataType": "bit",
                                    "RemoteColumnSql": "MA_LAB",
                                    "Description": "Labor RMA"
                                },
                                {
                                    "Name": "MA_OTA",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "MA_OTA",
                                    "Description": "Total Shipped \u002B Received"
                                },
                                {
                                    "Name": "MA_PRO",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "MA_PRO",
                                    "Description": "Product ID"
                                },
                                {
                                    "Name": "MA_REC",
                                    "DataType": "int",
                                    "RemoteColumnSql": "MA_REC",
                                    "Description": "Received Quantity"
                                },
                                {
                                    "Name": "MA_RET",
                                    "DataType": "bit",
                                    "RemoteColumnSql": "MA_RET",
                                    "Description": "Returned RMA"
                                },
                                {
                                    "Name": "MA_RMA",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "MA_RMA",
                                    "Description": "RMA Line Status"
                                },
                                {
                                    "Name": "MA_SER",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "MA_SER",
                                    "Description": "Serial Number"
                                },
                                {
                                    "Name": "MA_SHI",
                                    "DataType": "int",
                                    "RemoteColumnSql": "MA_SHI",
                                    "Description": "Shipped Quantity"
                                }
                            ]
                        },
                        {
                            "Name": "MA_OD",
                            "RemoteTableSql": "landscapeQuery_strategy_A.MA_OD",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "MAId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "MAId",
                                    "Description": ""
                                },
                                {
                                    "Name": "ODId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "ODId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "MA_RM",
                            "RemoteTableSql": "landscapeQuery_strategy_A.MA_RM",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "MAId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "MAId",
                                    "Description": ""
                                },
                                {
                                    "Name": "RMId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "RMId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "NE",
                            "RemoteTableSql": "landscapeQuery_strategy_A.NE",
                            "Description": "Initiatives*",
                            "Columns": [
                                {
                                    "Name": "NEId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "NEId",
                                    "Description": ""
                                },
                                {
                                    "Name": "NE_INI",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "NE_INI",
                                    "Description": "Initiative ID"
                                }
                            ]
                        },
                        {
                            "Name": "OD",
                            "RemoteTableSql": "landscapeQuery_strategy_A.OD",
                            "Description": "Product IDs",
                            "Columns": [
                                {
                                    "Name": "ODId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "ODId",
                                    "Description": ""
                                },
                                {
                                    "Name": "OD_MOD",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "OD_MOD",
                                    "Description": "Model Name"
                                },
                                {
                                    "Name": "OD_SKU",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "OD_SKU",
                                    "Description": "SKU List Price"
                                }
                            ]
                        },
                        {
                            "Name": "OD_PH",
                            "RemoteTableSql": "landscapeQuery_strategy_A.OD_PH",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "ODId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "ODId",
                                    "Description": ""
                                },
                                {
                                    "Name": "PHId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "PHId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "OD_PR",
                            "RemoteTableSql": "landscapeQuery_strategy_A.OD_PR",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "ODId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "ODId",
                                    "Description": ""
                                },
                                {
                                    "Name": "PRId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "PRId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "OF",
                            "RemoteTableSql": "landscapeQuery_strategy_A.OF",
                            "Description": "Installed Software",
                            "Columns": [
                                {
                                    "Name": "OFId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "OFId",
                                    "Description": ""
                                },
                                {
                                    "Name": "OF_CUR",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "OF_CUR",
                                    "Description": "Version History"
                                },
                                {
                                    "Name": "OF_DOF",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "OF_DOF",
                                    "Description": "End of Vulnerability / Security Support (EoVSS)"
                                },
                                {
                                    "Name": "OF_ECO",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "OF_ECO",
                                    "Description": "Recommendation Created"
                                },
                                {
                                    "Name": "OF_END",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "OF_END",
                                    "Description": "End of Software Maintenance (EoSWM)"
                                },
                                {
                                    "Name": "OF_LAS",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "OF_LAS",
                                    "Description": "SW  LDOS"
                                },
                                {
                                    "Name": "OF_NDO",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "OF_NDO",
                                    "Description": "End of Sale (EoSale)"
                                },
                                {
                                    "Name": "OF_OFT",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "OF_OFT",
                                    "Description": "Installation Software Compliance"
                                },
                                {
                                    "Name": "OF_REC",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "OF_REC",
                                    "Description": "Recommended SW Version"
                                },
                                {
                                    "Name": "OF_SWI",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "OF_SWI",
                                    "Description": "SW Installation Date"
                                },
                                {
                                    "Name": "OF_SWR",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "OF_SWR",
                                    "Description": "SW Replaced Date"
                                },
                                {
                                    "Name": "OF_SWT",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "OF_SWT",
                                    "Description": "SW Track"
                                },
                                {
                                    "Name": "OF_TRA",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "OF_TRA",
                                    "Description": "Track Description"
                                },
                                {
                                    "Name": "OF_UID",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "OF_UID",
                                    "Description": "Unique Key"
                                },
                                {
                                    "Name": "OF_URR",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "OF_URR",
                                    "Description": "Current SW EOX Milestone"
                                }
                            ]
                        },
                        {
                            "Name": "OF_PH",
                            "RemoteTableSql": "landscapeQuery_strategy_A.OF_PH",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "OFId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "OFId",
                                    "Description": ""
                                },
                                {
                                    "Name": "PHId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "PHId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "OF_SO",
                            "RemoteTableSql": "landscapeQuery_strategy_A.OF_SO",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "OFId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "OFId",
                                    "Description": ""
                                },
                                {
                                    "Name": "SOId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "SOId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "PH",
                            "RemoteTableSql": "landscapeQuery_strategy_A.PH",
                            "Description": "All Components",
                            "Columns": [
                                {
                                    "Name": "PHId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "PHId",
                                    "Description": ""
                                },
                                {
                                    "Name": "PH_ACT",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "PH_ACT",
                                    "Description": "Active Status"
                                },
                                {
                                    "Name": "PH_CUR",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "PH_CUR",
                                    "Description": "Current EOX Milestone"
                                },
                                {
                                    "Name": "PH_DAT",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "PH_DAT",
                                    "Description": "Device Found in Data Source(s)"
                                },
                                {
                                    "Name": "PH_DEC",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "PH_DEC",
                                    "Description": "Decommissioned Date"
                                },
                                {
                                    "Name": "PH_DEV",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "PH_DEV",
                                    "Description": "Device SubType"
                                },
                                {
                                    "Name": "PH_HOS",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "PH_HOS",
                                    "Description": "Host Name"
                                },
                                {
                                    "Name": "PH_INS",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "PH_INS",
                                    "Description": "Instance ID (C3)"
                                },
                                {
                                    "Name": "PH_IPA",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "PH_IPA",
                                    "Description": "IP Address"
                                },
                                {
                                    "Name": "PH_LDO",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "PH_LDO",
                                    "Description": "HW LDOS"
                                },
                                {
                                    "Name": "PH_LOG",
                                    "DataType": "bit",
                                    "RemoteColumnSql": "PH_LOG",
                                    "Description": "Parent Chassis (NEN)"
                                },
                                {
                                    "Name": "PH_NEW",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "PH_NEW",
                                    "Description": "Cost of Device"
                                },
                                {
                                    "Name": "PH_ONT",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "PH_ONT",
                                    "Description": "Contract Number"
                                },
                                {
                                    "Name": "PH_REC",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "PH_REC",
                                    "Description": "Recommended PID (EoL)"
                                },
                                {
                                    "Name": "PH_SER",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "PH_SER",
                                    "Description": "Serial Number"
                                },
                                {
                                    "Name": "PH_SHI",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "PH_SHI",
                                    "Description": "Shipped Date"
                                },
                                {
                                    "Name": "PH_UID",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "PH_UID",
                                    "Description": "Unique Key"
                                }
                            ]
                        },
                        {
                            "Name": "PH_SE",
                            "RemoteTableSql": "landscapeQuery_strategy_A.PH_SE",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "PHId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "PHId",
                                    "Description": ""
                                },
                                {
                                    "Name": "SEId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "SEId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "PH_SI",
                            "RemoteTableSql": "landscapeQuery_strategy_A.PH_SI",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "PHId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "PHId",
                                    "Description": ""
                                },
                                {
                                    "Name": "SIId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "SIId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "PH_VI",
                            "RemoteTableSql": "landscapeQuery_strategy_A.PH_VI",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "PHId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "PHId",
                                    "Description": ""
                                },
                                {
                                    "Name": "VIId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "VIId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "PR",
                            "RemoteTableSql": "landscapeQuery_strategy_A.PR",
                            "Description": "Product Families",
                            "Columns": [
                                {
                                    "Name": "PRId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "PRId",
                                    "Description": ""
                                },
                                {
                                    "Name": "PR_DIS",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "PR_DIS",
                                    "Description": "Disruption Index"
                                },
                                {
                                    "Name": "PR_FAM",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "PR_FAM",
                                    "Description": "Family Name"
                                }
                            ]
                        },
                        {
                            "Name": "PS",
                            "RemoteTableSql": "landscapeQuery_strategy_A.PS",
                            "Description": "PSIRTs",
                            "Columns": [
                                {
                                    "Name": "PSId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "PSId",
                                    "Description": ""
                                },
                                {
                                    "Name": "PS_BUL",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "PS_BUL",
                                    "Description": "Bulletin Title"
                                },
                                {
                                    "Name": "PS_CVE",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "PS_CVE",
                                    "Description": "CVE ID"
                                },
                                {
                                    "Name": "PS_CVS",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "PS_CVS",
                                    "Description": "CVSS Base Score"
                                },
                                {
                                    "Name": "PS_LLE",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "PS_LLE",
                                    "Description": "Bulletin Last Updated"
                                },
                                {
                                    "Name": "PS_POT",
                                    "DataType": "int",
                                    "RemoteColumnSql": "PS_POT",
                                    "Description": "Total Potentially Vulnerable Devices"
                                },
                                {
                                    "Name": "PS_PSI",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "PS_PSI",
                                    "Description": "PSIRT ID"
                                },
                                {
                                    "Name": "PS_SEC",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "PS_SEC",
                                    "Description": "Security Impact Rating"
                                },
                                {
                                    "Name": "PS_TOT",
                                    "DataType": "int",
                                    "RemoteColumnSql": "PS_TOT",
                                    "Description": "Total Vulnerable Devices"
                                },
                                {
                                    "Name": "PS_ULL",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "PS_ULL",
                                    "Description": "Bulletin First Published"
                                }
                            ]
                        },
                        {
                            "Name": "PS_VI",
                            "RemoteTableSql": "landscapeQuery_strategy_A.PS_VI",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "PSId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "PSId",
                                    "Description": ""
                                },
                                {
                                    "Name": "VIId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "VIId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "RM",
                            "RemoteTableSql": "landscapeQuery_strategy_A.RM",
                            "Description": "RMAs",
                            "Columns": [
                                {
                                    "Name": "RMId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "RMId",
                                    "Description": ""
                                },
                                {
                                    "Name": "RM_AIL",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RM_AIL",
                                    "Description": "Failure Code Name"
                                },
                                {
                                    "Name": "RM_CIT",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RM_CIT",
                                    "Description": "City"
                                },
                                {
                                    "Name": "RM_COU",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RM_COU",
                                    "Description": "Country"
                                },
                                {
                                    "Name": "RM_FAI",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RM_FAI",
                                    "Description": "Failure Code"
                                },
                                {
                                    "Name": "RM_HEA",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RM_HEA",
                                    "Description": "Header ID"
                                },
                                {
                                    "Name": "RM_MAC",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "RM_MAC",
                                    "Description": "RMA Close Date"
                                },
                                {
                                    "Name": "RM_ONT",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RM_ONT",
                                    "Description": "Contractual Service Level Key"
                                },
                                {
                                    "Name": "RM_ORD",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RM_ORD",
                                    "Description": "Order Number"
                                },
                                {
                                    "Name": "RM_OTA",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "RM_OTA",
                                    "Description": "Total Boomerang Lines within Header"
                                },
                                {
                                    "Name": "RM_QUE",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RM_QUE",
                                    "Description": "Requested Service Level Description"
                                },
                                {
                                    "Name": "RM_REQ",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RM_REQ",
                                    "Description": "Requested Service Level Key"
                                },
                                {
                                    "Name": "RM_RMA",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "RM_RMA",
                                    "Description": "RMA Creation Date"
                                },
                                {
                                    "Name": "RM_STA",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RM_STA",
                                    "Description": "State"
                                },
                                {
                                    "Name": "RM_TRA",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RM_TRA",
                                    "Description": "Contractual Service Level Description"
                                },
                                {
                                    "Name": "RM_UPL",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RM_UPL",
                                    "Description": "Uplift Requested"
                                }
                            ]
                        },
                        {
                            "Name": "RM_SE",
                            "RemoteTableSql": "landscapeQuery_strategy_A.RM_SE",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "RMId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "RMId",
                                    "Description": ""
                                },
                                {
                                    "Name": "SEId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "SEId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "RO",
                            "RemoteTableSql": "landscapeQuery_strategy_A.RO",
                            "Description": "Deliverables",
                            "Columns": [
                                {
                                    "Name": "ROId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "ROId",
                                    "Description": ""
                                },
                                {
                                    "Name": "RO_ASS",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RO_ASS",
                                    "Description": "Assignee"
                                },
                                {
                                    "Name": "RO_CLI",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RO_CLI",
                                    "Description": "Client Contact"
                                },
                                {
                                    "Name": "RO_CRE",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "RO_CRE",
                                    "Description": "Created"
                                },
                                {
                                    "Name": "RO_DES",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RO_DES",
                                    "Description": "Description"
                                },
                                {
                                    "Name": "RO_DUE",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "RO_DUE",
                                    "Description": "Due"
                                },
                                {
                                    "Name": "RO_EPI",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RO_EPI",
                                    "Description": "Epic Link"
                                },
                                {
                                    "Name": "RO_LAB",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RO_LAB",
                                    "Description": "labels"
                                },
                                {
                                    "Name": "RO_OMM",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RO_OMM",
                                    "Description": "Comment History"
                                },
                                {
                                    "Name": "RO_OMP",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RO_OMP",
                                    "Description": "Components"
                                },
                                {
                                    "Name": "RO_PRI",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RO_PRI",
                                    "Description": "Priority"
                                },
                                {
                                    "Name": "RO_PRO",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RO_PRO",
                                    "Description": "Project ID"
                                },
                                {
                                    "Name": "RO_REC",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RO_REC",
                                    "Description": "Recent Comment"
                                },
                                {
                                    "Name": "RO_REP",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RO_REP",
                                    "Description": "Reporter"
                                },
                                {
                                    "Name": "RO_RES",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RO_RES",
                                    "Description": "Resolution"
                                },
                                {
                                    "Name": "RO_STA",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RO_STA",
                                    "Description": "Status"
                                },
                                {
                                    "Name": "RO_TYP",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "RO_TYP",
                                    "Description": "Type"
                                },
                                {
                                    "Name": "RO_UPD",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "RO_UPD",
                                    "Description": "Updated"
                                }
                            ]
                        },
                        {
                            "Name": "SE",
                            "RemoteTableSql": "landscapeQuery_strategy_A.SE",
                            "Description": "TAC SRs",
                            "Columns": [
                                {
                                    "Name": "SEId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "SEId",
                                    "Description": ""
                                },
                                {
                                    "Name": "SE_ASS",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SE_ASS",
                                    "Description": "Assigned Technology"
                                },
                                {
                                    "Name": "SE_AST",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SE_AST",
                                    "Description": "Last Update By"
                                },
                                {
                                    "Name": "SE_CLO",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "SE_CLO",
                                    "Description": "Close Date"
                                },
                                {
                                    "Name": "SE_CRE",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "SE_CRE",
                                    "Description": "Creation Date"
                                },
                                {
                                    "Name": "SE_CUS",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SE_CUS",
                                    "Description": "Customer Incident Number"
                                },
                                {
                                    "Name": "SE_ENG",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SE_ENG",
                                    "Description": "Engineer Email"
                                },
                                {
                                    "Name": "SE_FSO",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "SE_FSO",
                                    "Description": "FResolution Time (h)"
                                },
                                {
                                    "Name": "SE_IGN",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SE_IGN",
                                    "Description": "Assigned Product Family"
                                },
                                {
                                    "Name": "SE_INC",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SE_INC",
                                    "Description": "SR Number"
                                },
                                {
                                    "Name": "SE_ISO",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "SE_ISO",
                                    "Description": "ISolution Time (h)"
                                },
                                {
                                    "Name": "SE_ITI",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SE_ITI",
                                    "Description": "Initial Sev."
                                },
                                {
                                    "Name": "SE_LAS",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "SE_LAS",
                                    "Description": "Last Updated"
                                },
                                {
                                    "Name": "SE_NCI",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SE_NCI",
                                    "Description": "Incident Status"
                                },
                                {
                                    "Name": "SE_NIT",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "SE_NIT",
                                    "Description": "Initial Response Time (m)"
                                },
                                {
                                    "Name": "SE_NTR",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SE_NTR",
                                    "Description": "Contract Number"
                                },
                                {
                                    "Name": "SE_ONT",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SE_ONT",
                                    "Description": "Contact Name"
                                },
                                {
                                    "Name": "SE_OUT",
                                    "DataType": "int",
                                    "RemoteColumnSql": "SE_OUT",
                                    "Description": "Outage Indicator"
                                },
                                {
                                    "Name": "SE_PRO",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SE_PRO",
                                    "Description": "Problem Code"
                                },
                                {
                                    "Name": "SE_RES",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SE_RES",
                                    "Description": "Resolution Code"
                                },
                                {
                                    "Name": "SE_SRS",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SE_SRS",
                                    "Description": "SR Summary"
                                },
                                {
                                    "Name": "SE_SSI",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SE_SSI",
                                    "Description": "Assigned Sub Technology"
                                },
                                {
                                    "Name": "SE_TAC",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SE_TAC",
                                    "Description": "Contact Email"
                                },
                                {
                                    "Name": "SE_TIM",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "SE_TIM",
                                    "Description": "Time to Close (Days)"
                                },
                                {
                                    "Name": "SE_URR",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SE_URR",
                                    "Description": "Current Sev."
                                },
                                {
                                    "Name": "SE_XIM",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SE_XIM",
                                    "Description": "Maximum Sev."
                                }
                            ]
                        },
                        {
                            "Name": "SE_UT",
                            "RemoteTableSql": "landscapeQuery_strategy_A.SE_UT",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "SEId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "SEId",
                                    "Description": ""
                                },
                                {
                                    "Name": "UTId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "UTId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "SI",
                            "RemoteTableSql": "landscapeQuery_strategy_A.SI",
                            "Description": "Customer Locations",
                            "Columns": [
                                {
                                    "Name": "SIId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "SIId",
                                    "Description": ""
                                },
                                {
                                    "Name": "SI_ADD",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SI_ADD",
                                    "Description": "Address"
                                },
                                {
                                    "Name": "SI_CIT",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SI_CIT",
                                    "Description": "City"
                                },
                                {
                                    "Name": "SI_KEY",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SI_KEY",
                                    "Description": "Key Site"
                                },
                                {
                                    "Name": "SI_SIT",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SI_SIT",
                                    "Description": "Site ID"
                                },
                                {
                                    "Name": "SI_STA",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SI_STA",
                                    "Description": "State"
                                },
                                {
                                    "Name": "SI_LOC",
                                    "DataType": "int",
                                    "RemoteColumnSql": "SI_LOC",
                                    "Description": "Location"
                                }
                            ]
                        },
                        {
                            "Name": "SI_UN",
                            "RemoteTableSql": "landscapeQuery_strategy_A.SI_UN",
                            "Description": "",
                            "Columns": [
                                {
                                    "Name": "SIId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "SIId",
                                    "Description": ""
                                },
                                {
                                    "Name": "UNId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "UNId",
                                    "Description": ""
                                }
                            ]
                        },
                        {
                            "Name": "SO",
                            "RemoteTableSql": "landscapeQuery_strategy_A.SO",
                            "Description": "Software Versions",
                            "Columns": [
                                {
                                    "Name": "SOId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "SOId",
                                    "Description": ""
                                },
                                {
                                    "Name": "SO_AJO",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SO_AJO",
                                    "Description": "Major Version"
                                },
                                {
                                    "Name": "SO_DAT",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "SO_DAT",
                                    "Description": "Date First Seen"
                                },
                                {
                                    "Name": "SO_DES",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SO_DES",
                                    "Description": "Version Name"
                                },
                                {
                                    "Name": "SO_DIS",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "SO_DIS",
                                    "Description": "Disruption Index"
                                },
                                {
                                    "Name": "SO_INT",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "SO_INT",
                                    "Description": "Maintenance Version Transformed"
                                },
                                {
                                    "Name": "SO_MAI",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SO_MAI",
                                    "Description": "Maintenance Version"
                                },
                                {
                                    "Name": "SO_MAJ",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "SO_MAJ",
                                    "Description": "Major Version Transformed"
                                },
                                {
                                    "Name": "SO_OST",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "SO_OST",
                                    "Description": "OS Type"
                                }
                            ]
                        },
                        {
                            "Name": "UN",
                            "RemoteTableSql": "landscapeQuery_strategy_A.UN",
                            "Description": "Countries",
                            "Columns": [
                                {
                                    "Name": "UNId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "UNId",
                                    "Description": ""
                                },
                                {
                                    "Name": "UN_OFF",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "UN_OFF",
                                    "Description": "Country Name"
                                },
                                {
                                    "Name": "UN_REG",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "UN_REG",
                                    "Description": "Region"
                                },
                                {
                                    "Name": "UN_UNT",
                                    "DataType": "nvarchar(30)",
                                    "RemoteColumnSql": "UN_UNT",
                                    "Description": "Country Code"
                                }
                            ]
                        },
                        {
                            "Name": "UT",
                            "RemoteTableSql": "landscapeQuery_strategy_A.UT",
                            "Description": "Customer Value",
                            "Columns": [
                                {
                                    "Name": "UTId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "UTId",
                                    "Description": ""
                                },
                                {
                                    "Name": "UT_AYS",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "UT_AYS",
                                    "Description": "Days within a Week Case Free"
                                },
                                {
                                    "Name": "UT_IGH",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "UT_IGH",
                                    "Description": "High Value Cases"
                                },
                                {
                                    "Name": "UT_LEA",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "UT_LEA",
                                    "Description": "Lead Time"
                                },
                                {
                                    "Name": "UT_NAM",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "UT_NAM",
                                    "Description": "Name"
                                },
                                {
                                    "Name": "UT_ODU",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "UT_ODU",
                                    "Description": "Productivity Savings"
                                },
                                {
                                    "Name": "UT_OWV",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "UT_OWV",
                                    "Description": "Low Value Cases"
                                },
                                {
                                    "Name": "UT_REP",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "UT_REP",
                                    "Description": "Replacement Cost"
                                },
                                {
                                    "Name": "UT_ROD",
                                    "DataType": "float(53)",
                                    "RemoteColumnSql": "UT_ROD",
                                    "Description": "Productivity Savings - User Volume/Hour"
                                },
                                {
                                    "Name": "UT_TRA",
                                    "DataType": "datetime2(7)",
                                    "RemoteColumnSql": "UT_TRA",
                                    "Description": "Tracking Date"
                                }
                            ]
                        },
                        {
                            "Name": "VI",
                            "RemoteTableSql": "landscapeQuery_strategy_A.VI",
                            "Description": "PSIRT Vulnerability Records",
                            "Columns": [
                                {
                                    "Name": "VIId",
                                    "DataType": "bigint",
                                    "RemoteColumnSql": "VIId",
                                    "Description": ""
                                },
                                {
                                    "Name": "VI_PSI",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "VI_PSI",
                                    "Description": "PSIRT ID"
                                },
                                {
                                    "Name": "VI_UNI",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "VI_UNI",
                                    "Description": "Unique ID"
                                },
                                {
                                    "Name": "VI_VUL",
                                    "DataType": "nvarchar(4000)",
                                    "RemoteColumnSql": "VI_VUL",
                                    "Description": "Vulnerability of Device"
                                }
                            ]
                        }
                    ]
                }
                """)!);

        _schemaFriendlyNames = new Lazy<DatabaseConnectionOptions>(() => JsonSerializer.Deserialize<DatabaseConnectionOptions>("""
                                {
                        "ConnectionString": "Server=sql; Database=ANON_Ingest; User Id=sa; Password=S3cr3tP@ssw0rd5;TrustServerCertificate=True",
                        "Tables": [
                            {
                                "TableName": "BE",
                                "Name": "Best Practice Rules",
                                "RemoteTableSql": "landscapeQuery_strategy_A.BE",
                                "Description": "Best Practice Rules",
                                "Columns": [
                                    {
                                        "ColName": "BEId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "BEId",
                                        "Name": "BEId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "BE_BPR",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "BE_BPR",
                                        "Name": "BP Rule ID",
                                        "Description": "BP Rule ID"
                                    },
                                    {
                                        "ColName": "BE_CAV",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "BE_CAV",
                                        "Name": "Caveat",
                                        "Description": "Caveat"
                                    },
                                    {
                                        "ColName": "BE_COR",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "BE_COR",
                                        "Name": "Corrective Action",
                                        "Description": "Corrective Action"
                                    },
                                    {
                                        "ColName": "BE_CRE",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "BE_CRE",
                                        "Name": "Created Date",
                                        "Description": "Created Date"
                                    },
                                    {
                                        "ColName": "BE_DES",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "BE_DES",
                                        "Name": "Description",
                                        "Description": "Description"
                                    },
                                    {
                                        "ColName": "BE_OST",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "BE_OST",
                                        "Name": "OS Type",
                                        "Description": "OS Type"
                                    },
                                    {
                                        "ColName": "BE_PRI",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "BE_PRI",
                                        "Name": "Primary Technology",
                                        "Description": "Primary Technology"
                                    },
                                    {
                                        "ColName": "BE_REC",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "BE_REC",
                                        "Name": "Recommendation",
                                        "Description": "Recommendation"
                                    },
                                    {
                                        "ColName": "BE_RIS",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "BE_RIS",
                                        "Name": "Risk",
                                        "Description": "Risk"
                                    },
                                    {
                                        "ColName": "BE_RUL",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "BE_RUL",
                                        "Name": "Rule Title",
                                        "Description": "Rule Title"
                                    },
                                    {
                                        "ColName": "BE_SEC",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "BE_SEC",
                                        "Name": "Secondary Technology",
                                        "Description": "Secondary Technology"
                                    }
                                ]
                            },
                            {
                                "TableName": "BE_ES",
                                "Name": "BE_ES",
                                "RemoteTableSql": "landscapeQuery_strategy_A.BE_ES",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "BEId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "BEId",
                                        "Name": "BEId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "ESId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "ESId",
                                        "Name": "ESId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "CO",
                                "Name": "Contract List",
                                "RemoteTableSql": "landscapeQuery_strategy_A.CO",
                                "Description": "Contract List",
                                "Columns": [
                                    {
                                        "ColName": "COId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "COId",
                                        "Name": "COId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "CO_ONT",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "CO_ONT",
                                        "Name": "Contract Number",
                                        "Description": "Contract Number"
                                    }
                                ]
                            },
                            {
                                "TableName": "CO_PH",
                                "Name": "CO_PH",
                                "RemoteTableSql": "landscapeQuery_strategy_A.CO_PH",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "COId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "COId",
                                        "Name": "COId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "PHId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "PHId",
                                        "Name": "PHId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "CO_SE",
                                "Name": "CO_SE",
                                "RemoteTableSql": "landscapeQuery_strategy_A.CO_SE",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "COId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "COId",
                                        "Name": "COId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "SEId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "SEId",
                                        "Name": "SEId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "DE",
                                "Name": "Defects",
                                "RemoteTableSql": "landscapeQuery_strategy_A.DE",
                                "Description": "Defects",
                                "Columns": [
                                    {
                                        "ColName": "DEId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "DEId",
                                        "Name": "DEId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "DE_CTF",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "DE_CTF",
                                        "Name": "Defect Fixed Version",
                                        "Description": "Defect Fixed Version"
                                    },
                                    {
                                        "ColName": "DE_DEF",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "DE_DEF",
                                        "Name": "Defect Number",
                                        "Description": "Defect Number"
                                    },
                                    {
                                        "ColName": "DE_ECT",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "DE_ECT",
                                        "Name": "Defect Reported Version",
                                        "Description": "Defect Reported Version"
                                    },
                                    {
                                        "ColName": "DE_EFE",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "DE_EFE",
                                        "Name": "Defect Title",
                                        "Description": "Defect Title"
                                    },
                                    {
                                        "ColName": "DE_FEC",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "DE_FEC",
                                        "Name": "Defect Submitted On",
                                        "Description": "Defect Submitted On"
                                    }
                                ]
                            },
                            {
                                "TableName": "DE_SE",
                                "Name": "DE_SE",
                                "RemoteTableSql": "landscapeQuery_strategy_A.DE_SE",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "DEId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "DEId",
                                        "Name": "DEId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "SEId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "SEId",
                                        "Name": "SEId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "EC",
                                "Name": "Technologies",
                                "RemoteTableSql": "landscapeQuery_strategy_A.EC",
                                "Description": "Technologies",
                                "Columns": [
                                    {
                                        "ColName": "ECId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "ECId",
                                        "Name": "ECId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "EC_DIS",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "EC_DIS",
                                        "Name": "Disruption Index",
                                        "Description": "Disruption Index"
                                    },
                                    {
                                        "ColName": "EC_TEC",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "EC_TEC",
                                        "Name": "Technology Name",
                                        "Description": "Technology Name"
                                    }
                                ]
                            },
                            {
                                "TableName": "EC_PH",
                                "Name": "EC_PH",
                                "RemoteTableSql": "landscapeQuery_strategy_A.EC_PH",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "ECId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "ECId",
                                        "Name": "ECId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "PHId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "PHId",
                                        "Name": "PHId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "ES",
                                "Name": "Best Practice Exceptions",
                                "RemoteTableSql": "landscapeQuery_strategy_A.ES",
                                "Description": "Best Practice Exceptions",
                                "Columns": [
                                    {
                                        "ColName": "ESId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "ESId",
                                        "Name": "ESId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "ES_BPR",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "ES_BPR",
                                        "Name": "bpRuleID",
                                        "Description": "bpRuleID"
                                    },
                                    {
                                        "ColName": "ES_DEV",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "ES_DEV",
                                        "Name": "Device ID",
                                        "Description": "Device ID"
                                    },
                                    {
                                        "ColName": "ES_UNI",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "ES_UNI",
                                        "Name": "Unique ID",
                                        "Description": "Unique ID"
                                    }
                                ]
                            },
                            {
                                "TableName": "ES_PH",
                                "Name": "ES_PH",
                                "RemoteTableSql": "landscapeQuery_strategy_A.ES_PH",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "ESId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "ESId",
                                        "Name": "ESId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "PHId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "PHId",
                                        "Name": "PHId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "EV",
                                "Name": "Device History",
                                "RemoteTableSql": "landscapeQuery_strategy_A.EV",
                                "Description": "Device History",
                                "Columns": [
                                    {
                                        "ColName": "EVId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "EVId",
                                        "Name": "EVId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "EV_EOX",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "EV_EOX",
                                        "Name": "EOX Milestone",
                                        "Description": "EOX Milestone"
                                    },
                                    {
                                        "ColName": "EV_FRO",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "EV_FRO",
                                        "Name": "Historic Month",
                                        "Description": "Historic Month"
                                    },
                                    {
                                        "ColName": "EV_OPE",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "EV_OPE",
                                        "Name": "Operational Status",
                                        "Description": "Operational Status"
                                    },
                                    {
                                        "ColName": "EV_PSI",
                                        "DataType": "int",
                                        "RemoteColumnSql": "EV_PSI",
                                        "Name": "PSIRT Count",
                                        "Description": "PSIRT Count"
                                    },
                                    {
                                        "ColName": "EV_SHI",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "EV_SHI",
                                        "Name": "Shipped Date",
                                        "Description": "Shipped Date"
                                    },
                                    {
                                        "ColName": "EV_SRS",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "EV_SRS",
                                        "Name": "SRs as % of IB",
                                        "Description": "SRs as % of IB"
                                    },
                                    {
                                        "ColName": "EV_SWC",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "EV_SWC",
                                        "Name": "SW Conformance",
                                        "Description": "SW Conformance"
                                    },
                                    {
                                        "ColName": "EV_TOT",
                                        "DataType": "int",
                                        "RemoteColumnSql": "EV_TOT",
                                        "Name": "Total SRs on active Devices",
                                        "Description": "Total SRs on active Devices"
                                    },
                                    {
                                        "ColName": "EV_UID",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "EV_UID",
                                        "Name": "Unique Key",
                                        "Description": "Unique Key"
                                    }
                                ]
                            },
                            {
                                "TableName": "EV_PH",
                                "Name": "EV_PH",
                                "RemoteTableSql": "landscapeQuery_strategy_A.EV_PH",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "EVId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "EVId",
                                        "Name": "EVId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "PHId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "PHId",
                                        "Name": "PHId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "JP",
                                "Name": "Business Unit",
                                "RemoteTableSql": "landscapeQuery_strategy_A.JP",
                                "Description": "Business Unit",
                                "Columns": [
                                    {
                                        "ColName": "JPId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "JPId",
                                        "Name": "JPId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "JP_DIS",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "JP_DIS",
                                        "Name": "Disruption Index",
                                        "Description": "Disruption Index"
                                    },
                                    {
                                        "ColName": "JP_SER",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "JP_SER",
                                        "Name": "Unit Name",
                                        "Description": "Unit Name"
                                    }
                                ]
                            },
                            {
                                "TableName": "JP_PH",
                                "Name": "JP_PH",
                                "RemoteTableSql": "landscapeQuery_strategy_A.JP_PH",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "JPId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "JPId",
                                        "Name": "JPId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "PHId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "PHId",
                                        "Name": "PHId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "MA",
                                "Name": "Shipments & Returns",
                                "RemoteTableSql": "landscapeQuery_strategy_A.MA",
                                "Description": "Shipments & Returns",
                                "Columns": [
                                    {
                                        "ColName": "MAId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "MAId",
                                        "Name": "MAId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "MA_BOO",
                                        "DataType": "int",
                                        "RemoteColumnSql": "MA_BOO",
                                        "Name": "Boomerang Line Flag",
                                        "Description": "Boomerang Line Flag"
                                    },
                                    {
                                        "ColName": "MA_HIP",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "MA_HIP",
                                        "Name": "Ship-To Contact Name",
                                        "Description": "Ship-To Contact Name"
                                    },
                                    {
                                        "ColName": "MA_INE",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "MA_INE",
                                        "Name": "Line ID",
                                        "Description": "Line ID"
                                    },
                                    {
                                        "ColName": "MA_LAB",
                                        "DataType": "bit",
                                        "RemoteColumnSql": "MA_LAB",
                                        "Name": "Labor RMA",
                                        "Description": "Labor RMA"
                                    },
                                    {
                                        "ColName": "MA_OTA",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "MA_OTA",
                                        "Name": "Total Shipped + Received",
                                        "Description": "Total Shipped + Received"
                                    },
                                    {
                                        "ColName": "MA_PRO",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "MA_PRO",
                                        "Name": "Product ID",
                                        "Description": "Product ID"
                                    },
                                    {
                                        "ColName": "MA_REC",
                                        "DataType": "int",
                                        "RemoteColumnSql": "MA_REC",
                                        "Name": "Received Quantity",
                                        "Description": "Received Quantity"
                                    },
                                    {
                                        "ColName": "MA_RET",
                                        "DataType": "bit",
                                        "RemoteColumnSql": "MA_RET",
                                        "Name": "Returned RMA",
                                        "Description": "Returned RMA"
                                    },
                                    {
                                        "ColName": "MA_RMA",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "MA_RMA",
                                        "Name": "RMA Line Status",
                                        "Description": "RMA Line Status"
                                    },
                                    {
                                        "ColName": "MA_SER",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "MA_SER",
                                        "Name": "Serial Number",
                                        "Description": "Serial Number"
                                    },
                                    {
                                        "ColName": "MA_SHI",
                                        "DataType": "int",
                                        "RemoteColumnSql": "MA_SHI",
                                        "Name": "Shipped Quantity",
                                        "Description": "Shipped Quantity"
                                    }
                                ]
                            },
                            {
                                "TableName": "MA_OD",
                                "Name": "MA_OD",
                                "RemoteTableSql": "landscapeQuery_strategy_A.MA_OD",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "MAId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "MAId",
                                        "Name": "MAId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "ODId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "ODId",
                                        "Name": "ODId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "MA_RM",
                                "Name": "MA_RM",
                                "RemoteTableSql": "landscapeQuery_strategy_A.MA_RM",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "MAId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "MAId",
                                        "Name": "MAId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "RMId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "RMId",
                                        "Name": "RMId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "NE",
                                "Name": "Initiatives*",
                                "RemoteTableSql": "landscapeQuery_strategy_A.NE",
                                "Description": "Initiatives*",
                                "Columns": [
                                    {
                                        "ColName": "NEId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "NEId",
                                        "Name": "NEId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "NE_INI",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "NE_INI",
                                        "Name": "Initiative ID",
                                        "Description": "Initiative ID"
                                    }
                                ]
                            },
                            {
                                "TableName": "OD",
                                "Name": "Product IDs",
                                "RemoteTableSql": "landscapeQuery_strategy_A.OD",
                                "Description": "Product IDs",
                                "Columns": [
                                    {
                                        "ColName": "ODId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "ODId",
                                        "Name": "ODId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "OD_MOD",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "OD_MOD",
                                        "Name": "Model Name",
                                        "Description": "Model Name"
                                    },
                                    {
                                        "ColName": "OD_SKU",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "OD_SKU",
                                        "Name": "SKU List Price",
                                        "Description": "SKU List Price"
                                    }
                                ]
                            },
                            {
                                "TableName": "OD_PH",
                                "Name": "OD_PH",
                                "RemoteTableSql": "landscapeQuery_strategy_A.OD_PH",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "ODId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "ODId",
                                        "Name": "ODId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "PHId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "PHId",
                                        "Name": "PHId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "OD_PR",
                                "Name": "OD_PR",
                                "RemoteTableSql": "landscapeQuery_strategy_A.OD_PR",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "ODId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "ODId",
                                        "Name": "ODId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "PRId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "PRId",
                                        "Name": "PRId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "OF",
                                "Name": "Installed Software",
                                "RemoteTableSql": "landscapeQuery_strategy_A.OF",
                                "Description": "Installed Software",
                                "Columns": [
                                    {
                                        "ColName": "OFId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "OFId",
                                        "Name": "OFId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "OF_CUR",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "OF_CUR",
                                        "Name": "Version History",
                                        "Description": "Version History"
                                    },
                                    {
                                        "ColName": "OF_DOF",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "OF_DOF",
                                        "Name": "End of Vulnerability / Security Support (EoVSS)",
                                        "Description": "End of Vulnerability / Security Support (EoVSS)"
                                    },
                                    {
                                        "ColName": "OF_ECO",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "OF_ECO",
                                        "Name": "Recommendation Created",
                                        "Description": "Recommendation Created"
                                    },
                                    {
                                        "ColName": "OF_END",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "OF_END",
                                        "Name": "End of Software Maintenance (EoSWM)",
                                        "Description": "End of Software Maintenance (EoSWM)"
                                    },
                                    {
                                        "ColName": "OF_LAS",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "OF_LAS",
                                        "Name": "SW  LDOS",
                                        "Description": "SW  LDOS"
                                    },
                                    {
                                        "ColName": "OF_NDO",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "OF_NDO",
                                        "Name": "End of Sale (EoSale)",
                                        "Description": "End of Sale (EoSale)"
                                    },
                                    {
                                        "ColName": "OF_OFT",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "OF_OFT",
                                        "Name": "Installation Software Compliance",
                                        "Description": "Installation Software Compliance"
                                    },
                                    {
                                        "ColName": "OF_REC",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "OF_REC",
                                        "Name": "Recommended SW Version",
                                        "Description": "Recommended SW Version"
                                    },
                                    {
                                        "ColName": "OF_SWI",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "OF_SWI",
                                        "Name": "SW Installation Date",
                                        "Description": "SW Installation Date"
                                    },
                                    {
                                        "ColName": "OF_SWR",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "OF_SWR",
                                        "Name": "SW Replaced Date",
                                        "Description": "SW Replaced Date"
                                    },
                                    {
                                        "ColName": "OF_SWT",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "OF_SWT",
                                        "Name": "SW Track",
                                        "Description": "SW Track"
                                    },
                                    {
                                        "ColName": "OF_TRA",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "OF_TRA",
                                        "Name": "Track Description",
                                        "Description": "Track Description"
                                    },
                                    {
                                        "ColName": "OF_UID",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "OF_UID",
                                        "Name": "Unique Key",
                                        "Description": "Unique Key"
                                    },
                                    {
                                        "ColName": "OF_URR",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "OF_URR",
                                        "Name": "Current SW EOX Milestone",
                                        "Description": "Current SW EOX Milestone"
                                    }
                                ]
                            },
                            {
                                "TableName": "OF_PH",
                                "Name": "OF_PH",
                                "RemoteTableSql": "landscapeQuery_strategy_A.OF_PH",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "OFId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "OFId",
                                        "Name": "OFId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "PHId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "PHId",
                                        "Name": "PHId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "OF_SO",
                                "Name": "OF_SO",
                                "RemoteTableSql": "landscapeQuery_strategy_A.OF_SO",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "OFId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "OFId",
                                        "Name": "OFId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "SOId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "SOId",
                                        "Name": "SOId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "PH",
                                "Name": "All Components",
                                "RemoteTableSql": "landscapeQuery_strategy_A.PH",
                                "Description": "All Components",
                                "Columns": [
                                    {
                                        "ColName": "PHId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "PHId",
                                        "Name": "PHId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "PH_ACT",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "PH_ACT",
                                        "Name": "Active Status",
                                        "Description": "Active Status"
                                    },
                                    {
                                        "ColName": "PH_CUR",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "PH_CUR",
                                        "Name": "Current EOX Milestone",
                                        "Description": "Current EOX Milestone"
                                    },
                                    {
                                        "ColName": "PH_DAT",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "PH_DAT",
                                        "Name": "Device Found in Data Source(s)",
                                        "Description": "Device Found in Data Source(s)"
                                    },
                                    {
                                        "ColName": "PH_DEC",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "PH_DEC",
                                        "Name": "Decommissioned Date",
                                        "Description": "Decommissioned Date"
                                    },
                                    {
                                        "ColName": "PH_DEV",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "PH_DEV",
                                        "Name": "Device SubType",
                                        "Description": "Device SubType"
                                    },
                                    {
                                        "ColName": "PH_HOS",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "PH_HOS",
                                        "Name": "Host Name",
                                        "Description": "Host Name"
                                    },
                                    {
                                        "ColName": "PH_INS",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "PH_INS",
                                        "Name": "Instance ID (C3)",
                                        "Description": "Instance ID (C3)"
                                    },
                                    {
                                        "ColName": "PH_IPA",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "PH_IPA",
                                        "Name": "IP Address",
                                        "Description": "IP Address"
                                    },
                                    {
                                        "ColName": "PH_LDO",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "PH_LDO",
                                        "Name": "HW LDOS",
                                        "Description": "HW LDOS"
                                    },
                                    {
                                        "ColName": "PH_LOG",
                                        "DataType": "bit",
                                        "RemoteColumnSql": "PH_LOG",
                                        "Name": "Parent Chassis (NEN)",
                                        "Description": "Parent Chassis (NEN)"
                                    },
                                    {
                                        "ColName": "PH_NEW",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "PH_NEW",
                                        "Name": "Cost of Device",
                                        "Description": "Cost of Device"
                                    },
                                    {
                                        "ColName": "PH_ONT",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "PH_ONT",
                                        "Name": "Contract Number",
                                        "Description": "Contract Number"
                                    },
                                    {
                                        "ColName": "PH_REC",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "PH_REC",
                                        "Name": "Recommended PID (EoL)",
                                        "Description": "Recommended PID (EoL)"
                                    },
                                    {
                                        "ColName": "PH_SER",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "PH_SER",
                                        "Name": "Serial Number",
                                        "Description": "Serial Number"
                                    },
                                    {
                                        "ColName": "PH_SHI",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "PH_SHI",
                                        "Name": "Shipped Date",
                                        "Description": "Shipped Date"
                                    },
                                    {
                                        "ColName": "PH_UID",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "PH_UID",
                                        "Name": "Unique Key",
                                        "Description": "Unique Key"
                                    }
                                ]
                            },
                            {
                                "TableName": "PH_SE",
                                "Name": "PH_SE",
                                "RemoteTableSql": "landscapeQuery_strategy_A.PH_SE",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "PHId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "PHId",
                                        "Name": "PHId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "SEId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "SEId",
                                        "Name": "SEId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "PH_SI",
                                "Name": "PH_SI",
                                "RemoteTableSql": "landscapeQuery_strategy_A.PH_SI",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "PHId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "PHId",
                                        "Name": "PHId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "SIId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "SIId",
                                        "Name": "SIId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "PH_VI",
                                "Name": "PH_VI",
                                "RemoteTableSql": "landscapeQuery_strategy_A.PH_VI",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "PHId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "PHId",
                                        "Name": "PHId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "VIId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "VIId",
                                        "Name": "VIId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "PR",
                                "Name": "Product Families",
                                "RemoteTableSql": "landscapeQuery_strategy_A.PR",
                                "Description": "Product Families",
                                "Columns": [
                                    {
                                        "ColName": "PRId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "PRId",
                                        "Name": "PRId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "PR_DIS",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "PR_DIS",
                                        "Name": "Disruption Index",
                                        "Description": "Disruption Index"
                                    },
                                    {
                                        "ColName": "PR_FAM",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "PR_FAM",
                                        "Name": "Family Name",
                                        "Description": "Family Name"
                                    }
                                ]
                            },
                            {
                                "TableName": "PS",
                                "Name": "PSIRTs",
                                "RemoteTableSql": "landscapeQuery_strategy_A.PS",
                                "Description": "PSIRTs",
                                "Columns": [
                                    {
                                        "ColName": "PSId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "PSId",
                                        "Name": "PSId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "PS_BUL",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "PS_BUL",
                                        "Name": "Bulletin Title",
                                        "Description": "Bulletin Title"
                                    },
                                    {
                                        "ColName": "PS_CVE",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "PS_CVE",
                                        "Name": "CVE ID",
                                        "Description": "CVE ID"
                                    },
                                    {
                                        "ColName": "PS_CVS",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "PS_CVS",
                                        "Name": "CVSS Base Score",
                                        "Description": "CVSS Base Score"
                                    },
                                    {
                                        "ColName": "PS_LLE",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "PS_LLE",
                                        "Name": "Bulletin Last Updated",
                                        "Description": "Bulletin Last Updated"
                                    },
                                    {
                                        "ColName": "PS_POT",
                                        "DataType": "int",
                                        "RemoteColumnSql": "PS_POT",
                                        "Name": "Total Potentially Vulnerable Devices",
                                        "Description": "Total Potentially Vulnerable Devices"
                                    },
                                    {
                                        "ColName": "PS_PSI",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "PS_PSI",
                                        "Name": "PSIRT ID",
                                        "Description": "PSIRT ID"
                                    },
                                    {
                                        "ColName": "PS_SEC",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "PS_SEC",
                                        "Name": "Security Impact Rating",
                                        "Description": "Security Impact Rating"
                                    },
                                    {
                                        "ColName": "PS_TOT",
                                        "DataType": "int",
                                        "RemoteColumnSql": "PS_TOT",
                                        "Name": "Total Vulnerable Devices",
                                        "Description": "Total Vulnerable Devices"
                                    },
                                    {
                                        "ColName": "PS_ULL",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "PS_ULL",
                                        "Name": "Bulletin First Published",
                                        "Description": "Bulletin First Published"
                                    }
                                ]
                            },
                            {
                                "TableName": "PS_VI",
                                "Name": "PS_VI",
                                "RemoteTableSql": "landscapeQuery_strategy_A.PS_VI",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "PSId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "PSId",
                                        "Name": "PSId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "VIId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "VIId",
                                        "Name": "VIId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "RM",
                                "Name": "RMAs",
                                "RemoteTableSql": "landscapeQuery_strategy_A.RM",
                                "Description": "RMAs",
                                "Columns": [
                                    {
                                        "ColName": "RMId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "RMId",
                                        "Name": "RMId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "RM_AIL",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RM_AIL",
                                        "Name": "Failure Code Name",
                                        "Description": "Failure Code Name"
                                    },
                                    {
                                        "ColName": "RM_CIT",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RM_CIT",
                                        "Name": "City",
                                        "Description": "City"
                                    },
                                    {
                                        "ColName": "RM_COU",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RM_COU",
                                        "Name": "Country",
                                        "Description": "Country"
                                    },
                                    {
                                        "ColName": "RM_FAI",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RM_FAI",
                                        "Name": "Failure Code",
                                        "Description": "Failure Code"
                                    },
                                    {
                                        "ColName": "RM_HEA",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RM_HEA",
                                        "Name": "Header ID",
                                        "Description": "Header ID"
                                    },
                                    {
                                        "ColName": "RM_MAC",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "RM_MAC",
                                        "Name": "RMA Close Date",
                                        "Description": "RMA Close Date"
                                    },
                                    {
                                        "ColName": "RM_ONT",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RM_ONT",
                                        "Name": "Contractual Service Level Key",
                                        "Description": "Contractual Service Level Key"
                                    },
                                    {
                                        "ColName": "RM_ORD",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RM_ORD",
                                        "Name": "Order Number",
                                        "Description": "Order Number"
                                    },
                                    {
                                        "ColName": "RM_OTA",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "RM_OTA",
                                        "Name": "Total Boomerang Lines within Header",
                                        "Description": "Total Boomerang Lines within Header"
                                    },
                                    {
                                        "ColName": "RM_QUE",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RM_QUE",
                                        "Name": "Requested Service Level Description",
                                        "Description": "Requested Service Level Description"
                                    },
                                    {
                                        "ColName": "RM_REQ",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RM_REQ",
                                        "Name": "Requested Service Level Key",
                                        "Description": "Requested Service Level Key"
                                    },
                                    {
                                        "ColName": "RM_RMA",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "RM_RMA",
                                        "Name": "RMA Creation Date",
                                        "Description": "RMA Creation Date"
                                    },
                                    {
                                        "ColName": "RM_STA",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RM_STA",
                                        "Name": "State",
                                        "Description": "State"
                                    },
                                    {
                                        "ColName": "RM_TRA",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RM_TRA",
                                        "Name": "Contractual Service Level Description",
                                        "Description": "Contractual Service Level Description"
                                    },
                                    {
                                        "ColName": "RM_UPL",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RM_UPL",
                                        "Name": "Uplift Requested",
                                        "Description": "Uplift Requested"
                                    }
                                ]
                            },
                            {
                                "TableName": "RM_SE",
                                "Name": "RM_SE",
                                "RemoteTableSql": "landscapeQuery_strategy_A.RM_SE",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "RMId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "RMId",
                                        "Name": "RMId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "SEId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "SEId",
                                        "Name": "SEId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "RO",
                                "Name": "Deliverables",
                                "RemoteTableSql": "landscapeQuery_strategy_A.RO",
                                "Description": "Deliverables",
                                "Columns": [
                                    {
                                        "ColName": "ROId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "ROId",
                                        "Name": "ROId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "RO_ASS",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RO_ASS",
                                        "Name": "Assignee",
                                        "Description": "Assignee"
                                    },
                                    {
                                        "ColName": "RO_CLI",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RO_CLI",
                                        "Name": "Client Contact",
                                        "Description": "Client Contact"
                                    },
                                    {
                                        "ColName": "RO_CRE",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "RO_CRE",
                                        "Name": "Created",
                                        "Description": "Created"
                                    },
                                    {
                                        "ColName": "RO_DES",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RO_DES",
                                        "Name": "Description",
                                        "Description": "Description"
                                    },
                                    {
                                        "ColName": "RO_DUE",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "RO_DUE",
                                        "Name": "Due",
                                        "Description": "Due"
                                    },
                                    {
                                        "ColName": "RO_EPI",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RO_EPI",
                                        "Name": "Epic Link",
                                        "Description": "Epic Link"
                                    },
                                    {
                                        "ColName": "RO_LAB",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RO_LAB",
                                        "Name": "labels",
                                        "Description": "labels"
                                    },
                                    {
                                        "ColName": "RO_OMM",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RO_OMM",
                                        "Name": "Comment History",
                                        "Description": "Comment History"
                                    },
                                    {
                                        "ColName": "RO_OMP",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RO_OMP",
                                        "Name": "Components",
                                        "Description": "Components"
                                    },
                                    {
                                        "ColName": "RO_PRI",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RO_PRI",
                                        "Name": "Priority",
                                        "Description": "Priority"
                                    },
                                    {
                                        "ColName": "RO_PRO",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RO_PRO",
                                        "Name": "Project ID",
                                        "Description": "Project ID"
                                    },
                                    {
                                        "ColName": "RO_REC",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RO_REC",
                                        "Name": "Recent Comment",
                                        "Description": "Recent Comment"
                                    },
                                    {
                                        "ColName": "RO_REP",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RO_REP",
                                        "Name": "Reporter",
                                        "Description": "Reporter"
                                    },
                                    {
                                        "ColName": "RO_RES",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RO_RES",
                                        "Name": "Resolution",
                                        "Description": "Resolution"
                                    },
                                    {
                                        "ColName": "RO_STA",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RO_STA",
                                        "Name": "Status",
                                        "Description": "Status"
                                    },
                                    {
                                        "ColName": "RO_TYP",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "RO_TYP",
                                        "Name": "Type",
                                        "Description": "Type"
                                    },
                                    {
                                        "ColName": "RO_UPD",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "RO_UPD",
                                        "Name": "Updated",
                                        "Description": "Updated"
                                    }
                                ]
                            },
                            {
                                "TableName": "SE",
                                "Name": "TAC SRs",
                                "RemoteTableSql": "landscapeQuery_strategy_A.SE",
                                "Description": "TAC SRs",
                                "Columns": [
                                    {
                                        "ColName": "SEId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "SEId",
                                        "Name": "SEId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "SE_ASS",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SE_ASS",
                                        "Name": "Assigned Technology",
                                        "Description": "Assigned Technology"
                                    },
                                    {
                                        "ColName": "SE_AST",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SE_AST",
                                        "Name": "Last Update By",
                                        "Description": "Last Update By"
                                    },
                                    {
                                        "ColName": "SE_CLO",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "SE_CLO",
                                        "Name": "Close Date",
                                        "Description": "Close Date"
                                    },
                                    {
                                        "ColName": "SE_CRE",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "SE_CRE",
                                        "Name": "Creation Date",
                                        "Description": "Creation Date"
                                    },
                                    {
                                        "ColName": "SE_CUS",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SE_CUS",
                                        "Name": "Customer Incident Number",
                                        "Description": "Customer Incident Number"
                                    },
                                    {
                                        "ColName": "SE_ENG",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SE_ENG",
                                        "Name": "Engineer Email",
                                        "Description": "Engineer Email"
                                    },
                                    {
                                        "ColName": "SE_FSO",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "SE_FSO",
                                        "Name": "FResolution Time (h)",
                                        "Description": "FResolution Time (h)"
                                    },
                                    {
                                        "ColName": "SE_IGN",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SE_IGN",
                                        "Name": "Assigned Product Family",
                                        "Description": "Assigned Product Family"
                                    },
                                    {
                                        "ColName": "SE_INC",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SE_INC",
                                        "Name": "SR Number",
                                        "Description": "SR Number"
                                    },
                                    {
                                        "ColName": "SE_ISO",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "SE_ISO",
                                        "Name": "ISolution Time (h)",
                                        "Description": "ISolution Time (h)"
                                    },
                                    {
                                        "ColName": "SE_ITI",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SE_ITI",
                                        "Name": "Initial Sev.",
                                        "Description": "Initial Sev."
                                    },
                                    {
                                        "ColName": "SE_LAS",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "SE_LAS",
                                        "Name": "Last Updated",
                                        "Description": "Last Updated"
                                    },
                                    {
                                        "ColName": "SE_NCI",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SE_NCI",
                                        "Name": "Incident Status",
                                        "Description": "Incident Status"
                                    },
                                    {
                                        "ColName": "SE_NIT",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "SE_NIT",
                                        "Name": "Initial Response Time (m)",
                                        "Description": "Initial Response Time (m)"
                                    },
                                    {
                                        "ColName": "SE_NTR",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SE_NTR",
                                        "Name": "Contract Number",
                                        "Description": "Contract Number"
                                    },
                                    {
                                        "ColName": "SE_ONT",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SE_ONT",
                                        "Name": "Contact Name",
                                        "Description": "Contact Name"
                                    },
                                    {
                                        "ColName": "SE_OUT",
                                        "DataType": "int",
                                        "RemoteColumnSql": "SE_OUT",
                                        "Name": "Outage Indicator",
                                        "Description": "Outage Indicator"
                                    },
                                    {
                                        "ColName": "SE_PRO",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SE_PRO",
                                        "Name": "Problem Code",
                                        "Description": "Problem Code"
                                    },
                                    {
                                        "ColName": "SE_RES",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SE_RES",
                                        "Name": "Resolution Code",
                                        "Description": "Resolution Code"
                                    },
                                    {
                                        "ColName": "SE_SRS",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SE_SRS",
                                        "Name": "SR Summary",
                                        "Description": "SR Summary"
                                    },
                                    {
                                        "ColName": "SE_SSI",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SE_SSI",
                                        "Name": "Assigned Sub Technology",
                                        "Description": "Assigned Sub Technology"
                                    },
                                    {
                                        "ColName": "SE_TAC",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SE_TAC",
                                        "Name": "Contact Email",
                                        "Description": "Contact Email"
                                    },
                                    {
                                        "ColName": "SE_TIM",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "SE_TIM",
                                        "Name": "Time to Close (Days)",
                                        "Description": "Time to Close (Days)"
                                    },
                                    {
                                        "ColName": "SE_URR",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SE_URR",
                                        "Name": "Current Sev.",
                                        "Description": "Current Sev."
                                    },
                                    {
                                        "ColName": "SE_XIM",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SE_XIM",
                                        "Name": "Maximum Sev.",
                                        "Description": "Maximum Sev."
                                    }
                                ]
                            },
                            {
                                "TableName": "SE_UT",
                                "Name": "SE_UT",
                                "RemoteTableSql": "landscapeQuery_strategy_A.SE_UT",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "SEId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "SEId",
                                        "Name": "SEId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "UTId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "UTId",
                                        "Name": "UTId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "SI",
                                "Name": "Customer Locations",
                                "RemoteTableSql": "landscapeQuery_strategy_A.SI",
                                "Description": "Customer Locations",
                                "Columns": [
                                    {
                                        "ColName": "SIId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "SIId",
                                        "Name": "SIId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "SI_ADD",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SI_ADD",
                                        "Name": "Address",
                                        "Description": "Address"
                                    },
                                    {
                                        "ColName": "SI_CIT",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SI_CIT",
                                        "Name": "City",
                                        "Description": "City"
                                    },
                                    {
                                        "ColName": "SI_KEY",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SI_KEY",
                                        "Name": "Key Site",
                                        "Description": "Key Site"
                                    },
                                    {
                                        "ColName": "SI_SIT",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SI_SIT",
                                        "Name": "Site ID",
                                        "Description": "Site ID"
                                    },
                                    {
                                        "ColName": "SI_STA",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SI_STA",
                                        "Name": "State",
                                        "Description": "State"
                                    },
                                    {
                                        "ColName": "SI_LOC",
                                        "DataType": "int",
                                        "RemoteColumnSql": "SI_LOC",
                                        "Name": "Location",
                                        "Description": "Location"
                                    }
                                ]
                            },
                            {
                                "TableName": "SI_UN",
                                "Name": "SI_UN",
                                "RemoteTableSql": "landscapeQuery_strategy_A.SI_UN",
                                "Description": "",
                                "Columns": [
                                    {
                                        "ColName": "SIId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "SIId",
                                        "Name": "SIId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "UNId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "UNId",
                                        "Name": "UNId",
                                        "Description": ""
                                    }
                                ]
                            },
                            {
                                "TableName": "SO",
                                "Name": "Software Versions",
                                "RemoteTableSql": "landscapeQuery_strategy_A.SO",
                                "Description": "Software Versions",
                                "Columns": [
                                    {
                                        "ColName": "SOId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "SOId",
                                        "Name": "SOId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "SO_AJO",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SO_AJO",
                                        "Name": "Major Version",
                                        "Description": "Major Version"
                                    },
                                    {
                                        "ColName": "SO_DAT",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "SO_DAT",
                                        "Name": "Date First Seen",
                                        "Description": "Date First Seen"
                                    },
                                    {
                                        "ColName": "SO_DES",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SO_DES",
                                        "Name": "Version Name",
                                        "Description": "Version Name"
                                    },
                                    {
                                        "ColName": "SO_DIS",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "SO_DIS",
                                        "Name": "Disruption Index",
                                        "Description": "Disruption Index"
                                    },
                                    {
                                        "ColName": "SO_INT",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "SO_INT",
                                        "Name": "Maintenance Version Transformed",
                                        "Description": "Maintenance Version Transformed"
                                    },
                                    {
                                        "ColName": "SO_MAI",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SO_MAI",
                                        "Name": "Maintenance Version",
                                        "Description": "Maintenance Version"
                                    },
                                    {
                                        "ColName": "SO_MAJ",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "SO_MAJ",
                                        "Name": "Major Version Transformed",
                                        "Description": "Major Version Transformed"
                                    },
                                    {
                                        "ColName": "SO_OST",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "SO_OST",
                                        "Name": "OS Type",
                                        "Description": "OS Type"
                                    }
                                ]
                            },
                            {
                                "TableName": "UN",
                                "Name": "Countries",
                                "RemoteTableSql": "landscapeQuery_strategy_A.UN",
                                "Description": "Countries",
                                "Columns": [
                                    {
                                        "ColName": "UNId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "UNId",
                                        "Name": "UNId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "UN_OFF",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "UN_OFF",
                                        "Name": "Country Name",
                                        "Description": "Country Name"
                                    },
                                    {
                                        "ColName": "UN_REG",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "UN_REG",
                                        "Name": "Region",
                                        "Description": "Region"
                                    },
                                    {
                                        "ColName": "UN_UNT",
                                        "DataType": "nvarchar(30)",
                                        "RemoteColumnSql": "UN_UNT",
                                        "Name": "Country Code",
                                        "Description": "Country Code"
                                    }
                                ]
                            },
                            {
                                "TableName": "UT",
                                "Name": "Customer Value",
                                "RemoteTableSql": "landscapeQuery_strategy_A.UT",
                                "Description": "Customer Value",
                                "Columns": [
                                    {
                                        "ColName": "UTId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "UTId",
                                        "Name": "UTId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "UT_AYS",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "UT_AYS",
                                        "Name": "Days within a Week Case Free",
                                        "Description": "Days within a Week Case Free"
                                    },
                                    {
                                        "ColName": "UT_IGH",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "UT_IGH",
                                        "Name": "High Value Cases",
                                        "Description": "High Value Cases"
                                    },
                                    {
                                        "ColName": "UT_LEA",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "UT_LEA",
                                        "Name": "Lead Time",
                                        "Description": "Lead Time"
                                    },
                                    {
                                        "ColName": "UT_NAM",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "UT_NAM",
                                        "Name": "Name",
                                        "Description": "Name"
                                    },
                                    {
                                        "ColName": "UT_ODU",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "UT_ODU",
                                        "Name": "Productivity Savings",
                                        "Description": "Productivity Savings"
                                    },
                                    {
                                        "ColName": "UT_OWV",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "UT_OWV",
                                        "Name": "Low Value Cases",
                                        "Description": "Low Value Cases"
                                    },
                                    {
                                        "ColName": "UT_REP",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "UT_REP",
                                        "Name": "Replacement Cost",
                                        "Description": "Replacement Cost"
                                    },
                                    {
                                        "ColName": "UT_ROD",
                                        "DataType": "float(53)",
                                        "RemoteColumnSql": "UT_ROD",
                                        "Name": "Productivity Savings - User Volume/Hour",
                                        "Description": "Productivity Savings - User Volume/Hour"
                                    },
                                    {
                                        "ColName": "UT_TRA",
                                        "DataType": "datetime2(7)",
                                        "RemoteColumnSql": "UT_TRA",
                                        "Name": "Tracking Date",
                                        "Description": "Tracking Date"
                                    }
                                ]
                            },
                            {
                                "TableName": "VI",
                                "Name": "PSIRT Vulnerability Records",
                                "RemoteTableSql": "landscapeQuery_strategy_A.VI",
                                "Description": "PSIRT Vulnerability Records",
                                "Columns": [
                                    {
                                        "ColName": "VIId",
                                        "DataType": "bigint",
                                        "RemoteColumnSql": "VIId",
                                        "Name": "VIId",
                                        "Description": ""
                                    },
                                    {
                                        "ColName": "VI_PSI",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "VI_PSI",
                                        "Name": "PSIRT ID",
                                        "Description": "PSIRT ID"
                                    },
                                    {
                                        "ColName": "VI_UNI",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "VI_UNI",
                                        "Name": "Unique ID",
                                        "Description": "Unique ID"
                                    },
                                    {
                                        "ColName": "VI_VUL",
                                        "DataType": "nvarchar(4000)",
                                        "RemoteColumnSql": "VI_VUL",
                                        "Name": "Vulnerability of Device",
                                        "Description": "Vulnerability of Device"
                                    }
                                ]
                            }
                        ]
                    }
                """)!);
    }

    public class DatabaseConnectionOptions
    {
        public required string ConnectionString { get; set; }

        public DatabaseType Type { get; set; } = DatabaseType.MsSql;

        public List<DatabaseTable> Tables { get; set; } = [];
    }

    public class DatabaseTable
    {
        // this is the virtual table name to be used in the sql query
        public required string Name { get; set; }

        public List<DatabaseColumn> Columns { get; set; } = [];

        // description to pass along to help the llm understand the data
        public string? Description { get; set; }

        // this is eather a table name or a subselect representing the table
        public required string RemoteTableSql { get; set; }

        // per user we will allow defining table row filters to apply to each of these
        // per user we will allow defining column filters (to exclude visibility of the column at all)
        // per user we will allow defining table filter (to exclude visibility of the table at all)

        public VirtualTable AsVirtualTable()
        {
            return new VirtualTable(Name, RemoteTableSql)
            {
                Columns = Columns.Select(c => c.AsVirtualColumn()).ToList()
            };
        }
    }

    public class DatabaseColumn
    {
        public required string Name { get; set; }

        public required string DataType { get; set; }

        // could be a column name or even an expression (basically it will be injected in place of the column name).
        public required string RemoteColumnSql { get; set; }

        // description to pass along to help the llm understand the data
        public string? Description { get; set; }
        public bool ExcludeFromStarExpansion { get; set; } = false;

        public VirtualColumn AsVirtualColumn()
        {
            return new VirtualColumn(Name, DataType, true, RemoteColumnSql)
            {
                ExcludeFromExpansion = ExcludeFromStarExpansion,
            };
        }
    }

    public enum DatabaseType
    {
        Unknown,
        MsSql,
    }
}
