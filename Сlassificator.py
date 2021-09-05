import fitz
import re
import pytesseract
import cv2
import os
import time
import sys

# НЕОБЪОДИМЫЕ БИБЛИОТЕКИ
# !pip install pytesseract
# !pip install opencv-python
# !pip3 install PyMuPDF

# НАСТРОЙКА КЛАССИФИКАТОРА И ОБЪЯВЛЕНИЕ ПЕРЕМЕННЫХ
pytesseract.pytesseract.tesseract_cmd = r"C:\Program Files\Tesseract-OCR\tesseract.exe"
tessdata_dir_config = r'--tessdata-dir "C:\\Program Files\\Tesseract-OCR\\tessdata"'

names = {"Положение о СД": ['Положение о совете директоров', 'Председатель совета директоров', 
                            'Письменное мнение', 'Опросный лист', 'Уведомление о проведении Совета директоров'],
        'Устав_действующий': ['Устав' , 'Уставный капитал', 'Органы управления', 'Резервный фонд', 'Бюллетени'],
        'Бухгалтерская отчетность_форма 1': ['Бухгалтерский баланс', 'Форма по ОКУД 0710001', 
                                             'Дата', 'Актив', 'Пассив'],
        'Бухгалтерская отчетность_форма 2': ['Отчет о финансовых результатах', 'Форма по ОКУД 0710002',
                                             'Дата', 'Чистая прибыль', 'Налог на прибыль'],
        'Бухгалтерская отчетность_форма 1 _промежуточная': ['Бухгалтерский баланс', 'Форма по ОКУД 0710001',
                                                             'Дата', 'Актив', 'Пассив'],
        'Бухгалтерская отчетность_форма 2 _промежуточная': ['Отчет о финансовых результатах', 'Форма по ОКУД 0710002',
                                             'Дата', 'Чистая прибыль', 'Налог на прибыль'],
        'Аудиторское заключение': ['Аудиторское заключение', 'Сведения об аудируемом лице', 'Сведения об аудиторе',
                                   'Основание для выражения мнения', 'Ответственность аудитора'],
        'Описание_деятельности_ГК': ['Презентация компании', 'История компании', 'Обзор рынка', 'Обзор компании'],
        'Решение_назначение ЕИО': ['Протокол совета директоров', 'Дата составления протокола',
                                   'Избрание/назначение генерального директора', 'Итоги голосования', 'Принятое решение']}



freq_classes = {'Устав_действующий': 0,
                'Положение о СД': 0,
                'Бухгалтерская отчетность_форма 1': 0,
                'Бухгалтерская отчетность_форма 2': 0,
                'Бухгалтерская отчетность_форма 1 _промежуточная': 0,
                'Бухгалтерская отчетность_форма 2 _промежуточная': 0,
                'Аудиторское заключение': 0,
                'Описание_деятельности_ГК': 0,
                'Решение_назначение ЕИО': 0}


if __name__ == "__main__":
# ВВОДИМ ПУТЬ К ФАЙЛУ ДЛЯ КЛАССИФИКАЦИИ И СОЗДАЕМ ВРЕМЕННУЮ ДИРЕКТОРИЮ ДЛЯ ХРАНЕНИЯ ФАЙЛОВ
    path_to_file = sys.argv[1]
    path_to_dir = path_to_file.rpartition('\\')[0]
    pdf_document = fitz.open(path_to_file)
    os.chdir(path_to_dir)
    os.mkdir('pics')
    path_to_buffer_dir = path_to_dir + 'pics'
    os.chdir(path_to_buffer_dir)

# ПРЕОБРАЗУЕМ PDF В ИЗОБРАЖЕНИЕ
    for current_page in range(len(pdf_document)):
        for image in pdf_document.getPageImageList(current_page):
            xref = image[0]
            pix = fitz.Pixmap(pdf_document, xref)
            if pix.n < 5:
                pix.writePNG("page%s-%s.png" % (current_page, xref))
            else:
                pix1 = fitz.Pixmap(fitz.csRGB, pix)
                pix1.writePNG("page%s-%s.png" % (current_page, xref))
                pix1 = None
            pix = None


# СЧИТЫВАЕМ ТЕКСТ С ИЗОБРАЖЕНИЙ
    pages = os.listdir(path_to_buffer_dir)
    for page in pages:
        print('processing')
        try:
            img = cv2.imread(page)
            text_page = pytesseract.image_to_string(img, lang= 'rus', config=tessdata_dir_config)
            for key, values in names.items():
                for val in values:
                    if re.findall(val.lower(), text_page.lower()):
                        freq_classes[key] += 1

        except:
            pass
            # print('Не верифицированно, отправить в соответствующую папку')


    answer = sorted(freq_classes, key=lambda x: freq_classes.get(x), reverse=True)[0]
    print(f'Документ распознан, имя: {answer}')

os.chdir(path_to_dir)
import shutil
shutil.rmtree(path_to_buffer_dir)

# tests:
# python scrip_classificator.py D:\\Положение.pdf
# python scrip_classificator.py D:\\Аудиторское_заключение.pdf