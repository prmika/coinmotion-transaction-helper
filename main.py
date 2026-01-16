import os
from readers.CsvReader import read_csv
from readers.XlsReader import read_xls
from writers.XlsWriter import write_xls
from processor import create_tax_report

if __name__ == "__main__":
    input_folder = './input/'
    file_path = None

    # Find .xls files in the input folder, fallback to .csv
    xls_files = [file for file in os.listdir(input_folder) if file.endswith('.xls')]
    csv_files = [file for file in os.listdir(input_folder) if file.endswith('.csv')]

    if len(csv_files) < 1:
        raise ValueError(f"No .csv files found in input folder. Expected at least one, but found {len(csv_files)}: {csv_files}")

    if len(csv_files) > 1:
        raise ValueError(f"Multiple .csv files found in input folder. Expected exactly one, but found {len(csv_files)}: {csv_files}")
    
    file_path = os.path.join(input_folder, csv_files[0])

    if not file_path:
        raise ValueError("No .csv file found in the input folder")
    

    try:
        print(f"Reading file: {file_path}")
        objects = read_csv(file_path)

        print("Read successfully. Processing data...")
        result = create_tax_report(objects)

        print("Processing successful. Writing output to output/output.xlsx")
        
        write_xls(result)
        print("Done.")

    except Exception as e:
        print(f"Error processing file: {e}")
        raise
