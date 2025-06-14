import os
import xlrd
from datetime import datetime
import openpyxl

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

    
def write_xls(objects):
    if not objects:
        print("No objects to write")
        return
    try:
        wb = openpyxl.Workbook()
        ws = wb.active
        ws.append(['Date', 'Action Type', 'Crypto Aquired Amount', 'Unit Price', 'EUR Amount', 'Source', 'Crypto Name'])
        for obj in objects:
            for item in objects[obj]:
                ws.append([item['date'], item['actionType'], item['cryptoAquiredAmount'], item['unitPrice'], item['eurAmount'], item['source'], item['cryptoName']])
        
        output_folder = './output/'
        if not os.path.exists(output_folder):
            os.makedirs(output_folder)
        wb.save(os.path.join(output_folder, 'output.xlsx'))

    except Exception as e:
        print(f"Error writing XLSX file: {e}")
        return

def sort_by_date(rows):
    try:
        if len(rows) == 0:
            return rows
        else:
            return sorted(rows, key=lambda row: datetime.strptime(row['date'], '%d.%m.%Y %H:%M:00'))
    except Exception as e:
        print(f"Error sorting rows by date: {e}")

def create_objects(result_1, result_2, result_3):
    if not result_1 or not result_2:
        return {}

    objects = {}

    def process_rows(rows, action_type):
        for i in range(0, len(rows), 2):
            if i + 1 >= len(rows):
                continue  # Skip if there is no corresponding fiat row

            if len(rows[i]) < 7 or len(rows[i+1]) < 5:
                continue  # Skip rows that don't have enough columns
            
            # format date from d.m.yyyy hh:mm to dd.mm.yyyy hh:mm:00
            date = datetime.strptime(rows[i][0], '%d.%m.%Y %H:%M').strftime('%d.%m.%Y %H:%M:00')
            crypto_name = rows[i][1]
            crypto_amount = rows[i][4].replace('+', '').replace('-', '').replace('.',',').split()[0]
            eur_amount = rows[i+1][4].replace('+', '').replace('-', '').replace('.',',').split()[0]
            unit_price = rows[i][6].replace('.',',').split()[0] if len(rows[i]) > 6 and rows[i][6] else None

            obj = {
                'date': date,
                'actionType': action_type,
                'cryptoAquiredAmount': crypto_amount,
                'unitPrice': unit_price,
                'eurAmount': eur_amount,
                'source': 'Coinmotion',
                'cryptoName': crypto_name
            }
            if crypto_name not in objects:
                objects[crypto_name] = []
            objects[crypto_name].append(obj)

    process_rows(result_1, 'Myynti')
    process_rows(result_2, 'Osto')

    # Process result_3 for specific criteria
    # add more criteria if needed
    for row in result_3:
        if 'Sisäinen siirto' in row[2] and 'Valmis' in row[3]:
            date = datetime.strptime(row[0], '%d.%m.%Y %H:%M').strftime('%d.%m.%Y %H:%M:00')
            crypto_name = row[1]
            crypto_amount = row[4].replace('+', '').replace('-', '').replace('.',',').split()[0]
            if '+' in row[4]:
                action_type = 'Osto'
            elif '-' in row[4]:
                action_type = 'Myynti'
            else:
                action_type = 'Sisäinen siirto'
            eur_amount = 0
            unit_price = row[6].replace('.',',').split()[0] if len(row) > 6 and row[6] else None

            obj = {
                'date': date,
                'actionType': action_type,
                'cryptoAquiredAmount': crypto_amount,
                'unitPrice': unit_price,
                'eurAmount': eur_amount,
                'source': 'Coinmotion',
                'cryptoName': crypto_name
            }
            if crypto_name not in objects:
                objects[crypto_name] = []
            objects[crypto_name].append(obj)

    for obj in objects:
        objects[obj] = sort_by_date(objects[obj])

    return objects


if __name__ == "__main__":
    input_folder = './input/'
    file_path = None

    # Find .xls files in the input folder
    xls_files = [file for file in os.listdir(input_folder) if file.endswith('.xls')]
    
    if len(xls_files) == 0:
        print("No .xls file found in the input folder")
    elif len(xls_files) > 1:
        raise ValueError(f"Multiple .xls files found in input folder. Expected exactly one, but found {len(xls_files)}: {xls_files}")
    else:
        file_path = os.path.join(input_folder, xls_files[0])

    if file_path:
        # Read the xls file
        result_1, result_2, result_3 = read_xls(file_path)
        
        # Create objects from the results
        objects = create_objects(result_1, result_2, result_3)

        # Write the objects to a new xls file
        write_xls(objects)

        print("Done")
    else:
        print("No .xls file found in the input folder")
