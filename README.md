# coinmotion-transaction-helper

Tool for reading Coinmotion transactions, categorizing them, and generating an output file suitable for FIFO-based tax calculation.

```mermaid
flowchart TD;
    A[./input/*.xls] --> B[read_xls(file_path)];
    B --> C[create_objects(result_1, result_2, result_3)];
    C --> D[write_xls(objects)];
    D --> E[./output/output.xlsx];
```

## Usage

1. Place one or more .xls files in the ./input/ folder.
2. Run the Python script (main.py).
3. The processed results will appear in the ./output/ folder as output.xlsx.

## Features

- Reads .xls files containing Coinmotion transactions.
- Filters transactions by type (Myynti, Osto, Sisäinen siirto) and status (Valmis).
- Groups transactions by crypto type.
- Sorts transactions by date and exports them to Excel.

## Dependencies

- Python 3.x
- [xlrd](https://pypi.org/project/xlrd/)
- [openpyxl](https://pypi.org/project/openpyxl/)

## Installation

1. Install Python 3.x.
2. Clone or download this repository.
3. Navigate to the project directory and install dependencies:
   ```sh
   pip install -r requirements.txt
   ```
