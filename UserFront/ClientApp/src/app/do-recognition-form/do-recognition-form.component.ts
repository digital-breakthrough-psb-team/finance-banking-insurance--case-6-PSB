import { HttpClient, HttpParams, HttpRequest, HttpResponse } from '@angular/common/http';
import { Component, Inject, OnInit } from '@angular/core';


type FileRecord = {
  title: string;
  status: 'pending_uploading' | 'uploading' | 'done' | 'recognition' | 'error' | 'unrecognized';
  inProgress?: boolean;
  recordId?: string;
  nomenclature?: string;
  nomenclatureCode?: string;
}

type PostToRecognitionResponse = {
  recordId: string;
}

type CheckProcessingResponse = {
  recordId: string;
  status: 'pending_uploading' | 'uploading' | 'done' | 'recognition' | 'error' | 'unrecognized';
  nomenclatureId: string;
}
@Component({
  selector: 'app-do-recognition-form',
  templateUrl: './do-recognition-form.component.html',
  styleUrls: ['./do-recognition-form.component.css']
})
export class DoRecognitionFormComponent implements OnInit {

  public files: FileRecord[] = [
    // {title: 'file_6.pdf', status: 'pending_uploading', },
    // {title: 'file_4.pdf', status: 'uploading', inProgress: true},
    // {
    //   title: 'file_2.pdf',
    //   status: 'done',
    //   nomenclature: 'Бухгалтерская отчетность_форма 1',
    //   nomenclatureCode: '	4f501f4a-c665-4cc8-9715-6ed26e7819f2'
    // },
    // {title: 'file_1.pdf', status: 'recognition', inProgress: true},
    // {title: 'file_3.pdf', status: 'error'},
    // {title: 'file_5.pdf', status: 'unrecognized'},
  ];

  constructor(private http: HttpClient, @Inject('BASE_URL') private baseUrl: string) { }

  ngOnInit() {
  }

  takeFile(evt: Event){
    console.log('takeFile')

    const target = evt.target as HTMLInputElement;
    const file = target.files[0];

    if(this.files.some(r=>r.title === file.name)){
      return;
    }

    this.files.push({
      title: file.name,
      status: 'uploading',
      inProgress: true
    });

    let formData = new FormData();
    formData.append('file', file);

    let params = new HttpParams();

    const req = new HttpRequest('POST', `${this.baseUrl}api/documents/upload-and-recognize`, formData, {
      params,
      reportProgress: true
    });

    this.http.request(req)
      .subscribe(evt=>{
        if (evt instanceof HttpResponse){
          const fileRecord = this.files.find(f=>f.title === file.name);
          if(fileRecord){
            console.log(`upload file "${file.name} response was completed"`);
            if(evt.ok){
              fileRecord.status = "recognition";
              fileRecord.inProgress = true;
              const body = evt.body as PostToRecognitionResponse;
              fileRecord.recordId = body.recordId;

              this.trackFileProcessing(fileRecord);
            } else {
              fileRecord.status = 'error';
              fileRecord.inProgress = false;
            }
          }
        }
      })
  }

  async trackFileProcessing(fileRecord: FileRecord){
    console.log("trackFileProcessing")
    const req = new HttpRequest('GET', `${this.baseUrl}api/documents/upload-and-recognize/checkStatus/${fileRecord.recordId}`);
    this.http.request(req).subscribe(evt=>{
      console.log("trackFileProcessing check response: ", evt)
      if (evt instanceof HttpResponse && evt.ok){
        const body = evt.body as CheckProcessingResponse;
        fileRecord.status = body.status;
        fileRecord.nomenclatureCode = body.nomenclatureId;
        if(body.status == 'recognition'){
          setTimeout(()=>this.trackFileProcessing(fileRecord), 300);
        } else {
          fileRecord.inProgress = false;
        }
      }
    })
  }
}
