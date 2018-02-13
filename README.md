# Wolftales_to_fb2-winGUI
Program for parse translated novels from wolftales.ru to fb2 format.

File Novels.xml is additional. If file in directory with main exe then all novels will load from it and picture covers will be add to finalized fb2 book.


**sample of Novels.xml**

    <?xml version="1.0"?>
    <ArrayOfNovel xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
      <Novel>
        <BookName>Stellar Transformations</BookName>
		<Url>http://wolftales.ru/stellar-transformations</Url>
		<AuthorFName>IET</AuthorFName>
		<AuthorLName>I Eat Tomatoes</AuthorLName>
		<CoverImgB64>*jpeg cover encoded by base64*</CoverImgB64>
      </Novel>
    </ArrayOfNovel>