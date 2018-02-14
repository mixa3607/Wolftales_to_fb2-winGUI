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
		<CoverPath>CoversJpg\st.jpg</CoverPath><!--NOT necessary-->
		<CoverUrl>http://images.17173.com/2013/news/2013/12/12/zc1212xc01s.jpg</CoverUrl><!--NOT necessary-->
		<CoverImgB64>*jpeg cover encoded by base64*</CoverImgB64><!--NOT necessary-->
      </Novel>
	  ........
    </ArrayOfNovel>
if CoverPath is null or not exists then programm will download cover by CoverUrl. If downloading return expection then programm will use CoverImgB64. If CoverImgB64 is null then program will skip cover.