import xlrd
# Coinmotion uses csv files now. this does not work anymore.
# This code is kept for reference in case they switch back to xls files. But wont work straight away.
def read_xls(file_path):
    try:
        workbook = xlrd.open_workbook(file_path)
        sheet = workbook.sheet_by_index(0)
        
        if sheet.nrows == 0:
            print("No rows in the XLS file")
            return [], []
        
        result_1 = []  # Rows with 'Myynti' or 'myynti'
        result_2 = []  # Rows with 'Osto' or 'osto'
        result_3 = []  # Everything else

        header = sheet.row_values(0)
        type_idx = header.index('Type')
        status_idx = header.index('Status')

        for row_idx in range(1, sheet.nrows):
            row = sheet.row_values(row_idx)
            row_type = row[type_idx]
            if 'Myynti' in row_type or 'myynti' in row_type:
                if 'Valmis' in row[status_idx]:
                    result_1.append(row)
            elif 'Osto' in row_type or 'osto' in row_type:
                if 'Valmis' in row[status_idx] and 'Nosto' not in row_type:
                    result_2.append(row)
            else:
                result_3.append(row)

        return result_1, result_2, result_3

    except Exception as e:
        print(f"Error reading XLS file: {e}")