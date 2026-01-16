import openpyxl
import os

def write_xls(objects):
    if not objects:
        print("No objects to write")
        return
    try:
        wb = openpyxl.Workbook()
        ws = wb.active
        ws.append(['time', 'type', 'cryptoAmount', 'rate', 'eurAmount', 'source', 'fromCurrency', 'toCurrency', 'fee', 'feeCurrency'])

        for currency in objects:
            for item in objects[currency]['transactions']:
                ws.append([item['time'], item['type'], item['cryptoAmount'], item['rate'], item['eurAmount'], item['source'], item['fromCurrency'], item['toCurrency'], item['fee'], item['feeCurrency']])


        output_folder = './output/'
        if not os.path.exists(output_folder):
            os.makedirs(output_folder)
        wb.save(os.path.join(output_folder, 'output.xlsx'))

    except Exception as e:
        print(f"Error writing XLSX file: {e}")
        return