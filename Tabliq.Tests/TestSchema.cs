using System;
using System.Collections.Generic;
using System.Linq;
using Tabliq.Sql.Binding;
public class TestSchema : ISchemaProvider
{
    public static ISchemaProvider DatabaseSchemaAlt =>
        new TestSchema
        {
            Tables =
            {
                    new Table("TAC SRs")
                    {
                        Columns =
                        {
                            new Column("Incident Status", "nvarchar(4000)", true),
                            new Column("Initial Sev.", "nvarchar(4000)", true),
                            new Column("Current Sev.", "nvarchar(4000)", true)
                        }
                    }
            }
        };

    public static ISchemaProvider DatabaseSchema =>
        new TestSchema
        {
            Tables =
            {
                    new Table("BE_ALT")
                    {
                        Columns =
                        {
                            new Column("BEId", "bigint", false) { ExcludeFromExpansion = true },
                            new Column("BE_BPR", "nvarchar(4000)", true),
                            new Column("BE_CAV", "nvarchar(4000)", true),
                            new Column("BE_COR", "nvarchar(4000)", true),
                            new Column("BE_CRE", "datetime2(7)", true),
                            new Column("BE_DES", "nvarchar(4000)", true),
                            new Column("BE_OST", "nvarchar(4000)", true),
                            new Column("BE_PRI", "nvarchar(4000)", true),
                            new Column("BE_REC", "nvarchar(4000)", true),
                            new Column("BE_RIS", "nvarchar(4000)", true),
                            new Column("BE_RUL", "nvarchar(4000)", true),
                            new Column("BE_SEC", "nvarchar(4000)", true)
                        }
                    },
                    new Table("BE")
                    {
                        Columns =
                        {
                            new Column("BEId", "bigint", false) { ExcludeFromExpansion = true },
                            new Column("BE_BPR", "nvarchar(4000)", true),
                            new Column("BE_CAV", "nvarchar(4000)", true),
                            new Column("BE_COR", "nvarchar(4000)", true),
                            new Column("BE_CRE", "datetime2(7)", true),
                            new Column("BE_DES", "nvarchar(4000)", true),
                            new Column("BE_OST", "nvarchar(4000)", true),
                            new Column("BE_PRI", "nvarchar(4000)", true),
                            new Column("BE_REC", "nvarchar(4000)", true),
                            new Column("BE_RIS", "nvarchar(4000)", true),
                            new Column("BE_RUL", "nvarchar(4000)", true),
                            new Column("BE_SEC", "nvarchar(4000)", true)
                        }
                    },
                    new Table("BE_ES")
                    {
                        Columns =
                        {
                            new Column("BEId", "bigint", false),
                            new Column("ESId", "bigint", false)
                        }
                    },
                    new Table("CO")
                    {
                        Columns =
                        {
                            new Column("COId", "bigint", false) { ExcludeFromExpansion = true },
                            new Column("CO_ONT", "nvarchar(4000)", true)
                        }
                    },
                    new Table("CO_PH")
                    {
                        Columns =
                        {
                            new Column("COId", "bigint", false),
                            new Column("PHId", "bigint", false)
                        }
                    },
                    new Table("CO_SE")
                    {
                        Columns =
                        {
                            new Column("COId", "bigint", false),
                            new Column("SEId", "bigint", false)
                        }
                    },
                    new Table("DE")
                    {
                        Columns =
                        {
                            new Column("DEId", "bigint", false) { ExcludeFromExpansion = true },
                            new Column("DE_CTF", "nvarchar(4000)", true),
                            new Column("DE_DEF", "nvarchar(4000)", true),
                            new Column("DE_ECT", "nvarchar(4000)", true),
                            new Column("DE_EFE", "nvarchar(4000)", true),
                            new Column("DE_FEC", "datetime2(7)", true)
                        }
                    },
                    new Table("DE_SE")
                    {
                        Columns =
                        {
                            new Column("DEId", "bigint", false),
                            new Column("SEId", "bigint", false)
                        }
                    },
                    new Table("EC")
                    {
                        Columns =
                        {
                            new Column("ECId", "bigint", false) { ExcludeFromExpansion = true },
                            new Column("EC_DIS", "float(53)", true),
                            new Column("EC_TEC", "nvarchar(4000)", true)
                        }
                    },
                    new Table("EC_PH")
                    {
                        Columns =
                        {
                            new Column("ECId", "bigint", false),
                            new Column("PHId", "bigint", false)
                        }
                    },
                    new Table("ES")
                    {
                        Columns =
                        {
                            new Column("ESId", "bigint", false),
                            new Column("ES_BPR", "nvarchar(4000)", true),
                            new Column("ES_DEV", "nvarchar(4000)", true),
                            new Column("ES_UNI", "nvarchar(4000)", true)
                        }
                    },
                    new Table("ES_PH")
                    {
                        Columns =
                        {
                            new Column("ESId", "bigint", false),
                            new Column("PHId", "bigint", false)
                        }
                    },
                    new Table("EV")
                    {
                        Columns =
                        {
                            new Column("EVId", "bigint", false),
                            new Column("EV_EOX", "nvarchar(4000)", true),
                            new Column("EV_FRO", "datetime2(7)", true),
                            new Column("EV_OPE", "nvarchar(4000)", true),
                            new Column("EV_PSI", "int", true),
                            new Column("EV_SHI", "datetime2(7)", true),
                            new Column("EV_SRS", "float(53)", true),
                            new Column("EV_SWC", "nvarchar(4000)", true),
                            new Column("EV_TOT", "int", true),
                            new Column("EV_UID", "nvarchar(4000)", true)
                        }
                    },
                    new Table("EV_PH")
                    {
                        Columns =
                        {
                            new Column("EVId", "bigint", false),
                            new Column("PHId", "bigint", false)
                        }
                    },
                    new Table("JP")
                    {
                        Columns =
                        {
                            new Column("JPId", "bigint", false),
                            new Column("JP_DIS", "float(53)", true),
                            new Column("JP_SER", "nvarchar(4000)", true)
                        }
                    },
                    new Table("JP_PH")
                    {
                        Columns =
                        {
                            new Column("JPId", "bigint", false),
                            new Column("PHId", "bigint", false)
                        }
                    },
                    new Table("MA")
                    {
                        Columns =
                        {
                            new Column("MAId", "bigint", false),
                            new Column("MA_BOO", "int", true),
                            new Column("MA_HIP", "nvarchar(4000)", true),
                            new Column("MA_INE", "nvarchar(4000)", true),
                            new Column("MA_LAB", "bit", true),
                            new Column("MA_OTA", "float(53)", true),
                            new Column("MA_PRO", "nvarchar(4000)", true),
                            new Column("MA_REC", "int", true),
                            new Column("MA_RET", "bit", true),
                            new Column("MA_RMA", "nvarchar(4000)", true),
                            new Column("MA_SER", "nvarchar(4000)", true),
                            new Column("MA_SHI", "int", true)
                        }
                    },
                    new Table("MA_OD")
                    {
                        Columns =
                        {
                            new Column("MAId", "bigint", false),
                            new Column("ODId", "bigint", false)
                        }
                    },
                    new Table("MA_RM")
                    {
                        Columns =
                        {
                            new Column("MAId", "bigint", false),
                            new Column("RMId", "bigint", false)
                        }
                    },
                    new Table("NE")
                    {
                        Columns =
                        {
                            new Column("NEId", "bigint", false),
                            new Column("NE_INI", "nvarchar(4000)", true)
                        }
                    },
                    new Table("OD")
                    {
                        Columns =
                        {
                            new Column("ODId", "bigint", false),
                            new Column("OD_MOD", "nvarchar(4000)", true),
                            new Column("OD_SKU", "float(53)", true)
                        }
                    },
                    new Table("OD_PH")
                    {
                        Columns =
                        {
                            new Column("ODId", "bigint", false),
                            new Column("PHId", "bigint", false)
                        }
                    },
                    new Table("OD_PR")
                    {
                        Columns =
                        {
                            new Column("ODId", "bigint", false),
                            new Column("PRId", "bigint", false)
                        }
                    },
                    new Table("OF")
                    {
                        Columns =
                        {
                            new Column("OFId", "bigint", false),
                            new Column("OF_CUR", "nvarchar(4000)", true),
                            new Column("OF_DOF", "datetime2(7)", true),
                            new Column("OF_ECO", "datetime2(7)", true),
                            new Column("OF_END", "datetime2(7)", true),
                            new Column("OF_LAS", "datetime2(7)", true),
                            new Column("OF_NDO", "datetime2(7)", true),
                            new Column("OF_OFT", "nvarchar(4000)", true),
                            new Column("OF_REC", "nvarchar(4000)", true),
                            new Column("OF_SWI", "datetime2(7)", true),
                            new Column("OF_SWR", "datetime2(7)", true),
                            new Column("OF_SWT", "nvarchar(4000)", true),
                            new Column("OF_TRA", "nvarchar(4000)", true),
                            new Column("OF_UID", "nvarchar(4000)", true),
                            new Column("OF_URR", "nvarchar(4000)", true)
                        }
                    },
                    new Table("OF_PH")
                    {
                        Columns =
                        {
                            new Column("OFId", "bigint", false),
                            new Column("PHId", "bigint", false)
                        }
                    },
                    new Table("OF_SO")
                    {
                        Columns =
                        {
                            new Column("OFId", "bigint", false),
                            new Column("SOId", "bigint", false)
                        }
                    },
                    new Table("PH")
                    {
                        Columns =
                        {
                            new Column("PHId", "bigint", false),
                            new Column("PH_ACT", "nvarchar(4000)", true),
                            new Column("PH_CUR", "nvarchar(4000)", true),
                            new Column("PH_DAT", "nvarchar(4000)", true),
                            new Column("PH_DEC", "datetime2(7)", true),
                            new Column("PH_DEV", "nvarchar(4000)", true),
                            new Column("PH_HOS", "nvarchar(4000)", true),
                            new Column("PH_INS", "nvarchar(4000)", true),
                            new Column("PH_IPA", "nvarchar(4000)", true),
                            new Column("PH_LDO", "datetime2(7)", true),
                            new Column("PH_LOG", "bit", true),
                            new Column("PH_NEW", "float(53)", true),
                            new Column("PH_ONT", "nvarchar(4000)", true),
                            new Column("PH_REC", "nvarchar(4000)", true),
                            new Column("PH_SER", "nvarchar(4000)", true),
                            new Column("PH_SHI", "datetime2(7)", true),
                            new Column("PH_UID", "nvarchar(4000)", true)
                        }
                    },
                    new Table("PH_SE")
                    {
                        Columns =
                        {
                            new Column("PHId", "bigint", false),
                            new Column("SEId", "bigint", false)
                        }
                    },
                    new Table("PH_SI")
                    {
                        Columns =
                        {
                            new Column("PHId", "bigint", false),
                            new Column("SIId", "bigint", false)
                        }
                    },
                    new Table("PH_VI")
                    {
                        Columns =
                        {
                            new Column("PHId", "bigint", false),
                            new Column("VIId", "bigint", false)
                        }
                    },
                    new Table("PR")
                    {
                        Columns =
                        {
                            new Column("PRId", "bigint", false),
                            new Column("PR_DIS", "float(53)", true),
                            new Column("PR_FAM", "nvarchar(4000)", true)
                        }
                    },
                    new Table("PS")
                    {
                        Columns =
                        {
                            new Column("PSId", "bigint", false),
                            new Column("PS_BUL", "nvarchar(4000)", true),
                            new Column("PS_CVE", "nvarchar(4000)", true),
                            new Column("PS_CVS", "float(53)", true),
                            new Column("PS_LLE", "datetime2(7)", true),
                            new Column("PS_POT", "int", true),
                            new Column("PS_PSI", "nvarchar(4000)", true),
                            new Column("PS_SEC", "nvarchar(4000)", true),
                            new Column("PS_TOT", "int", true),
                            new Column("PS_ULL", "datetime2(7)", true)
                        }
                    },
                    new Table("PS_VI")
                    {
                        Columns =
                        {
                            new Column("PSId", "bigint", false),
                            new Column("VIId", "bigint", false)
                        }
                    },
                    new Table("RM")
                    {
                        Columns =
                        {
                            new Column("RMId", "bigint", false),
                            new Column("RM_AIL", "nvarchar(4000)", true),
                            new Column("RM_CIT", "nvarchar(4000)", true),
                            new Column("RM_COU", "nvarchar(4000)", true),
                            new Column("RM_FAI", "nvarchar(4000)", true),
                            new Column("RM_HEA", "nvarchar(4000)", true),
                            new Column("RM_MAC", "datetime2(7)", true),
                            new Column("RM_ONT", "nvarchar(4000)", true),
                            new Column("RM_ORD", "nvarchar(4000)", true),
                            new Column("RM_OTA", "float(53)", true),
                            new Column("RM_QUE", "nvarchar(4000)", true),
                            new Column("RM_REQ", "nvarchar(4000)", true),
                            new Column("RM_RMA", "datetime2(7)", true),
                            new Column("RM_STA", "nvarchar(4000)", true),
                            new Column("RM_TRA", "nvarchar(4000)", true),
                            new Column("RM_UPL", "nvarchar(4000)", true)
                        }
                    },
                    new Table("RM_SE")
                    {
                        Columns =
                        {
                            new Column("RMId", "bigint", false),
                            new Column("SEId", "bigint", false)
                        }
                    },
                    new Table("RO")
                    {
                        Columns =
                        {
                            new Column("ROId", "bigint", false),
                            new Column("RO_ASS", "nvarchar(4000)", true),
                            new Column("RO_CLI", "nvarchar(4000)", true),
                            new Column("RO_CRE", "datetime2(7)", true),
                            new Column("RO_DES", "nvarchar(4000)", true),
                            new Column("RO_DUE", "datetime2(7)", true),
                            new Column("RO_EPI", "nvarchar(4000)", true),
                            new Column("RO_LAB", "nvarchar(4000)", true),
                            new Column("RO_OMM", "nvarchar(4000)", true),
                            new Column("RO_OMP", "nvarchar(4000)", true),
                            new Column("RO_PRI", "nvarchar(4000)", true),
                            new Column("RO_PRO", "nvarchar(4000)", true),
                            new Column("RO_REC", "nvarchar(4000)", true),
                            new Column("RO_REP", "nvarchar(4000)", true),
                            new Column("RO_RES", "nvarchar(4000)", true),
                            new Column("RO_STA", "nvarchar(4000)", true),
                            new Column("RO_TYP", "nvarchar(4000)", true),
                            new Column("RO_UPD", "datetime2(7)", true)
                        }
                    },
                    new Table("SE")
                    {
                        Columns =
                        {
                            new Column("SEId", "bigint", false),
                            new Column("SE_ASS", "nvarchar(4000)", true),
                            new Column("SE_AST", "nvarchar(4000)", true),
                            new Column("SE_CLO", "datetime2(7)", true),
                            new Column("SE_CRE", "datetime2(7)", true),
                            new Column("SE_CUS", "nvarchar(4000)", true),
                            new Column("SE_ENG", "nvarchar(4000)", true),
                            new Column("SE_FSO", "float(53)", true),
                            new Column("SE_IGN", "nvarchar(4000)", true),
                            new Column("SE_INC", "nvarchar(4000)", true),
                            new Column("SE_ISO", "float(53)", true),
                            new Column("SE_ITI", "nvarchar(4000)", true),
                            new Column("SE_LAS", "datetime2(7)", true),
                            new Column("SE_NCI", "nvarchar(4000)", true),
                            new Column("SE_NIT", "float(53)", true),
                            new Column("SE_NTR", "nvarchar(4000)", true),
                            new Column("SE_ONT", "nvarchar(4000)", true),
                            new Column("SE_OUT", "int", true),
                            new Column("SE_PRO", "nvarchar(4000)", true),
                            new Column("SE_RES", "nvarchar(4000)", true),
                            new Column("SE_SRS", "nvarchar(4000)", true),
                            new Column("SE_SSI", "nvarchar(4000)", true),
                            new Column("SE_TAC", "nvarchar(4000)", true),
                            new Column("SE_TIM", "float(53)", true),
                            new Column("SE_URR", "nvarchar(4000)", true),
                            new Column("SE_XIM", "nvarchar(4000)", true)
                        }
                    },
                    new Table("SE_UT")
                    {
                        Columns =
                        {
                            new Column("SEId", "bigint", false),
                            new Column("UTId", "bigint", false)
                        }
                    },
                    new Table("SI")
                    {
                        Columns =
                        {
                            new Column("SIId", "bigint", false),
                            new Column("SI_ADD", "nvarchar(4000)", true),
                            new Column("SI_CIT", "nvarchar(4000)", true),
                            new Column("SI_KEY", "nvarchar(4000)", true),
                            new Column("SI_SIT", "nvarchar(4000)", true),
                            new Column("SI_STA", "nvarchar(4000)", true),
                            new Column("SI_LOC", "int", true)
                        }
                    },
                    new Table("SI_UN")
                    {
                        Columns =
                        {
                            new Column("SIId", "bigint", false),
                            new Column("UNId", "bigint", false)
                        }
                    },
                    new Table("SO")
                    {
                        Columns =
                        {
                            new Column("SOId", "bigint", false),
                            new Column("SO_AJO", "nvarchar(4000)", true),
                            new Column("SO_DAT", "datetime2(7)", true),
                            new Column("SO_DES", "nvarchar(4000)", true),
                            new Column("SO_DIS", "float(53)", true),
                            new Column("SO_INT", "float(53)", true),
                            new Column("SO_MAI", "nvarchar(4000)", true),
                            new Column("SO_MAJ", "float(53)", true),
                            new Column("SO_OST", "nvarchar(4000)", true)
                        }
                    },
                    new Table("UN")
                    {
                        Columns =
                        {
                            new Column("UNId", "bigint", false),
                            new Column("UN_OFF", "nvarchar(4000)", true),
                            new Column("UN_REG", "nvarchar(4000)", true),
                            new Column("UN_UNT", "nvarchar(30)", true)
                        }
                    },
                    new Table("UT")
                    {
                        Columns =
                        {
                            new Column("UTId", "bigint", false),
                            new Column("UT_AYS", "float(53)", true),
                            new Column("UT_IGH", "float(53)", true),
                            new Column("UT_LEA", "float(53)", true),
                            new Column("UT_NAM", "nvarchar(4000)", true),
                            new Column("UT_ODU", "float(53)", true),
                            new Column("UT_OWV", "float(53)", true),
                            new Column("UT_REP", "float(53)", true),
                            new Column("UT_ROD", "float(53)", true),
                            new Column("UT_TRA", "datetime2(7)", true)
                        }
                    },
                    new Table("VI")
                    {
                        Columns =
                        {
                            new Column("VIId", "bigint", false),
                            new Column("VI_PSI", "nvarchar(4000)", true),
                            new Column("VI_UNI", "nvarchar(4000)", true),
                            new Column("VI_VUL", "nvarchar(4000)", true)
                        }
                    }
            }
        };

    public List<Table> Tables { get; set; } = [];

    public List<string> Parameters { get; set; } = [];

    public List<FunctionSymbol> Functions { get; set; } = [];

    public FunctionSymbol? GetFunction(string name)
    {
        return Functions.FirstOrDefault(f => f.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }

    public ParameterSymbol? GetParameter(string name)
    {
        var pName = Parameters.FirstOrDefault(p => p.Equals(name, System.StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(pName))
        {
            return null;
        }
        return new ParameterSymbol(pName, string.Empty, false);
    }

    public TableSymbol? GetTable(string name)
    {
        return Tables?.FirstOrDefault(x => x.TableName.Equals(name, StringComparison.OrdinalIgnoreCase))?.AsSymbol();
    }
}


public class CombineSchema : ISchemaProvider
{
    private readonly ISchemaProvider _inner1;
    private readonly ISchemaProvider _inner2;
    public CombineSchema(ISchemaProvider inner1, ISchemaProvider inner2)
    {
        _inner1 = inner1;
        _inner2 = inner2;
    }
    public ParameterSymbol? GetParameter(string name)
        => _inner1.GetParameter(name) ?? _inner2.GetParameter(name);
    public TableSymbol? GetTable(string name) => _inner1.GetTable(name) ?? _inner2.GetTable(name);
    public FunctionSymbol? GetFunction(string name) => _inner1.GetFunction(name) ?? _inner2.GetFunction(name);
}

public class Table
{
    public Table(string tableName)
    {
        TableName = tableName;
    }

    public TableSymbol AsSymbol()
        => new TableSymbol(TableName, Columns.Select(x => x.AsSymbol()).ToList());

    public string TableName { get; }

    public List<Column> Columns { get; set; } = [];
}
public class Column
{
    public Column(string colName, string dataType, bool isNullable)
    {
        ColumnName = colName;
        DataType = dataType;
        IsNullable = isNullable;
    }
    public ColumnSymbol AsSymbol()
        => new ColumnSymbol(ColumnName, DataType);

    public string ColumnName { get; }
    public string DataType { get; }
    public bool IsNullable { get; }
    public bool ExcludeFromExpansion { get; init; }

}


